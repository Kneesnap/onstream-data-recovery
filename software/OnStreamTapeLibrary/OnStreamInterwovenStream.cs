using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// This class is a <see cref="Stream"/> which moves between various different streams containing tape dumps.
    /// The moving between different streams is done based on which stream contains the next tape block.
    /// This is used for taking different tape dumps and automatically organizing the blocks inside of them into the desired read order.
    /// </summary>
    public class OnStreamInterwovenStream : Stream
    {
        public readonly ImmutableList<OnStreamTapeBlock> Blocks;
        private readonly byte[] _buffer;
        private int _bufferPos;
        private long _currentBlock = -1;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => (this.Blocks.Count * OnStreamDataStream.DataSectionSize);
        public const int BufferLength = (int)OnStreamDataStream.DataSectionSize;

        public override long Position {
            get => (this._currentBlock * OnStreamDataStream.DataSectionSize) + this._bufferPos;
            set => this.Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        /// Gets the block currently active in the buffer.
        /// </summary>
        public OnStreamTapeBlock CurrentBlock => this.Blocks[(int)Math.Max(0, this._currentBlock)];

        public OnStreamInterwovenStream(List<OnStreamTapeBlock> tapeBlocks) {
            List<OnStreamTapeBlock> blocks = new List<OnStreamTapeBlock>(tapeBlocks);
            tapeBlocks.RemoveAll(block => block.Signature == OnStreamDataStream.WriteStopSignatureNumber);
            this.Blocks = blocks.ToImmutableList();
            this._buffer = new byte[BufferLength];
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

            if (block >= this.Blocks.Count) {
                return (remainder > 0 || block > this.Blocks.Count)
                    ? "<ERROR: PAST END OF INTERWOVEN DATA>"
                    : "<END OF INTERWOVEN DATA>";
            } else {
                OnStreamTapeBlock tapeBlock = this.Blocks[block];
                long fileIndex = tapeBlock.Index + remainder;
                return $"{tapeBlock.File.Stream.GetFileIndexDisplay(fileIndex)}/{tapeBlock.File.FileName}/{tapeBlock.PhysicalBlock:X8}";
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
            if (this._currentBlock + 1 >= this.Blocks.Count)
                throw new EndOfStreamException("Attempted to read beyond the last provided tape block.");
            
            OnStreamTapeBlock nextTapeBlock = this.Blocks[(int)++this._currentBlock];
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
        
        /// <summary>
        /// Test if the reader skipped any blocks due to them not being present since the given index.
        /// </summary>
        /// <param name="reader">The reader to test.</param>
        /// <param name="startIndex">The earliest reader position index which an error could be found at.</param>
        /// <param name="resumeAfterError">If there was an error, and this is true, the reader will start reading at the first position after the error.</param>
        /// <param name="blocksSkipped">The number of blocks skipped since the provided reader index.</param>
        /// <returns>Whether any missing data was skipped.</returns>
        /// <exception cref="InvalidCastException">Thrown if the reader is not reading an interwoven stream.</exception>
        public static bool WasMissingDataSkipped(this DataReader reader, long startIndex, bool resumeAfterError, out int blocksSkipped) {
            return WasMissingDataSkipped(reader, startIndex, resumeAfterError, out blocksSkipped, out _);
        }

        /// <summary>
        /// Test if the reader skipped any blocks due to them not being present since the given index.
        /// </summary>
        /// <param name="reader">The reader to test.</param>
        /// <param name="startIndex">The earliest reader position index which an error could be found at.</param>
        /// <param name="resumeAfterError">If there was an error, and this is true, the reader will start reading at the first position after the error.</param>
        /// <param name="blocksSkipped">The number of blocks skipped since the provided reader index.</param>
        /// <param name="lastValidBlock">Output storage for the last valid block.</param>
        /// <returns>Whether any missing data was skipped.</returns>
        /// <exception cref="InvalidCastException">Thrown if the reader is not reading an interwoven stream.</exception>
        public static bool WasMissingDataSkipped(this DataReader reader, long startIndex, bool resumeAfterError, out int blocksSkipped, out OnStreamTapeBlock lastValidBlock) {
            if (reader.BaseStream is not OnStreamInterwovenStream interwovenStream)
                throw new InvalidCastException($"The reader is attached to {reader.BaseStream.GetTypeDisplayName()}, not an {nameof(OnStreamInterwovenStream)}.");
            if (startIndex > reader.Index)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"The provided index {reader.GetFileIndexDisplay(startIndex)} comes after the current reader index, {reader.GetFileIndexDisplay()}.");

            lastValidBlock = reader.GetCurrentTapeBlock();
            int oldBlockPos = (int)(startIndex / OnStreamInterwovenStream.BufferLength);
            int newBlockPos = (int)(reader.Index / OnStreamInterwovenStream.BufferLength);

            blocksSkipped = 0;
            for (int i = oldBlockPos; i <= newBlockPos; i++) {
                OnStreamTapeBlock block = interwovenStream.Blocks[i];
                if (blocksSkipped == 0)
                    lastValidBlock = block;
                
                if (block.MissingBlockCount > 0) {
                    if (blocksSkipped == 0 && resumeAfterError)
                        reader.Index = ((i + 1) * OnStreamDataStream.DataSectionSize);

                    blocksSkipped += block.MissingBlockCount;
                }
            }

            return blocksSkipped > 0;
        }
    }
}