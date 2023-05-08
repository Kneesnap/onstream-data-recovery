using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.IO;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// This class is a <see cref="Stream"/> which moves between various different streams containing tape dumps.
    /// This allows continuous reading of the data which 
    /// </summary>
    public class OnStreamInterwovenStream : Stream
    {
        private readonly List<OnStreamTapeBlock> _blocks;
        private readonly byte[] _buffer;
        private int _bufferPos;
        private long _currentBlock = -1;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => (this._blocks.Count * OnStreamDataStream.DataSectionSize);

        public override long Position {
            get => (this._currentBlock * OnStreamDataStream.DataSectionSize) + this._bufferPos;
            set => this.Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// Gets the block currently active in the buffer.
        /// </summary>
        public OnStreamTapeBlock CurrentBlock => this._blocks[(int)Math.Max(0, this._currentBlock)];

        public OnStreamInterwovenStream(TapeConfig tapeConfig, Dictionary<uint, OnStreamTapeBlock> blocks) {
            this._blocks = CreateOrderedBlockList(tapeConfig, blocks);
            this._buffer = new byte[OnStreamDataStream.DataSectionSize];
            this._bufferPos = this._buffer.Length;
        }

        /// <summary>
        /// Formats an index into this file into a display string displaying the position in the file.
        /// </summary>
        /// <param name="index">The index to format</param>
        /// <returns>friendly display string</returns>
        public string FormatIndex(long index) {
            if (index < 0)
                return $"<ERROR: Negative index {index}>";

            int block = (int)(index / OnStreamDataStream.DataSectionSize);
            long remainder = (index % OnStreamDataStream.DataSectionSize);

            if (block >= this._blocks.Count) {
                return (remainder > 0 || block > this._blocks.Count)
                    ? "<ERROR: PAST END OF INTERWOVEN DATA>"
                    : "<END OF INTERWOVEN DATA>";
            } else {
                OnStreamTapeBlock tapeBlock = this._blocks[block];
                long fileIndex = tapeBlock.Index + remainder;
                return $"{tapeBlock.File.Stream.GetFileIndexDisplay(fileIndex)}/{tapeBlock.File.FileName}";
            }
        }

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush() {
            // Do nothing..?
        }

        /// <inheritdoc cref="Stream.Read(byte[],int,int)"/>
        public override int Read(byte[] buffer, int offset, int count) {
            int bytesRead = 0;
            while (count > 0) {
                int availableBytes = (this._buffer.Length - this._bufferPos);

                if (availableBytes > 0) {
                    int copiedBytes = Math.Min(count, availableBytes);
                    Array.Copy(this._buffer, this._bufferPos, buffer, offset, copiedBytes);
                    this._bufferPos += copiedBytes;
                    count -= copiedBytes;
                    offset += copiedBytes;
                    bytesRead += copiedBytes;
                }

                if (count > 0 && this._bufferPos >= this._buffer.Length)
                    this.ReadNextSection();
            }

            return bytesRead;
        }

        private void ReadNextSection() {
            if (this._currentBlock + 1 >= this._blocks.Count)
                throw new EndOfStreamException();

            OnStreamTapeBlock nextTapeBlock = this._blocks[(int)++this._currentBlock];
            this._bufferPos %= this._buffer.Length;
            nextTapeBlock.File.Stream.Position = nextTapeBlock.Index;
            nextTapeBlock.File.Stream.Read(this._buffer, 0, this._buffer.Length);
        }

        private long SeekRelative(long offset) {
            long oldPosition = this.Position;
            if (offset == 0)
                return oldPosition; // No change.

            long newPosition = oldPosition + offset;
            if (newPosition < 0)
                throw new ArgumentOutOfRangeException($"Cannot seek to absolute position {newPosition}.");

            long oldBufferPos = this._bufferPos;
            long oldBlock = this._currentBlock + (this._bufferPos / this._buffer.Length);
            long newBlock = (newPosition / this._buffer.Length);

            this._currentBlock = newBlock;
            this._bufferPos = (int)(newPosition % this._buffer.Length);
            if (oldBlock != newBlock || oldBufferPos >= this._buffer.Length) {
                this._bufferPos += this._buffer.Length; // It's not the same block, or the block wasn't read, so let's mark this as needing to be read.
                this._currentBlock--;
            }

            return newPosition;
        }

        /// <inheritdoc cref="Stream.Seek"/>
        public override long Seek(long offset, SeekOrigin origin) {
            return origin switch {
                SeekOrigin.Begin => this.SeekRelative(offset - this.Position),
                SeekOrigin.Current => this.SeekRelative(offset),
                SeekOrigin.End => throw new ArgumentOutOfRangeException(nameof(origin), "Seeking from the end is not supported."),
                _ => throw new ArgumentOutOfRangeException(nameof(origin), $"Unknown SeekOrigin '{origin}'")
            };
        }

        /// <inheritdoc cref="Stream.SetLength"/>
        public override void SetLength(long value) {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="Stream.Write(byte[],int,int)"/>
        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an ordered list of tape blocks from a mapping of all the tape blocks.
        /// </summary>
        /// <param name="tape">The configuration of the tape dumps to create it with.</param>
        /// <param name="blockMapping">The mapping of tape blocks to read from.</param>
        /// <returns>orderedList</returns>
        private static List<OnStreamTapeBlock> CreateOrderedBlockList(TapeConfig tape, Dictionary<uint, OnStreamTapeBlock> blockMapping) {
            List<OnStreamTapeBlock> blocks = new List<OnStreamTapeBlock>();

            OnStreamPhysicalPosition.FromLogicalBlock(0, out OnStreamPhysicalPosition position);

            for (int i = 0; i < blockMapping.Count; i++) {
                // Search until the next position with data is found.
                OnStreamTapeBlock? nextTapeBlock;
                do {
                    if (!ArcServe.TryIncrementBlockIncludeParkingZone(in position, out position))
                        throw new EndOfStreamException();
                } while (!blockMapping.TryGetValue(position.ToPhysicalBlock(), out nextTapeBlock));

                if (!tape.SkippedPhysicalBlocks.Contains(nextTapeBlock.PhysicalBlock)) 
                    blocks.Add(nextTapeBlock);
            }

            return blocks;
        }
    }

    public static class DataReaderExtensions
    {
        /// <summary>
        /// Gets the current tape block which the reader is reading.
        /// </summary>
        /// <param name="reader">The reader to get from.</param>
        /// <returns>currentTapeBlock</returns>
        /// <exception cref="InvalidCastException">Thrown if the reader is not reading an interwoven stream.</exception>
        public static OnStreamTapeBlock GetCurrentTapeBlock(this DataReader reader) {
            if (reader.BaseStream is not OnStreamInterwovenStream interwovenStream)
                throw new InvalidCastException($"The reader is attached to {reader.BaseStream.GetTypeDisplayName()}, not an {nameof(OnStreamInterwovenStream)}.");
            return interwovenStream.CurrentBlock;
        }
    }
}