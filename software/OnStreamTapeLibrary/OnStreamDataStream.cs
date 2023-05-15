using ModToolFramework.Utils.Data;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// This is a wrapper around <see cref="Stream"/>, allowing continuous user data to be read.
    /// In other words, this will strip out the OnStream AUX data, to allow for reading continuously.
    /// </summary>
    public class OnStreamDataStream : Stream
    {
        private readonly Stream _stream;

        public override bool CanRead => this._stream.CanRead;
        public override bool CanSeek => this._stream.CanSeek;
        public override bool CanWrite => this._stream.CanWrite;
        public override long Length => (this._stream.Length / FullSectionSize) * DataSectionSize;

        /// <inheritdoc cref="Stream.Position"/>
        public override long Position {
            get => RemoveAuxSectionsFromIndex(this._stream.Position);
            set => this.Seek(value, SeekOrigin.Begin);
        }

        public const long DataSectionSize = 32768;
        public const long AuxSectionSize = 512;
        public const long FullSectionSize = DataSectionSize + AuxSectionSize;
        
        /// <summary>
        /// This appears in the "Application Signature" aux field for tapes written by both ArcServe and Retrospect.
        /// It seems to occur on both 30 + 50GB tapes, on tapes written with different software, and isn't very well understood.
        /// Sometimes the sections contain real data, sometimes they do not.
        /// </summary>
        public const string WriteStopSignature = "WTST";
        public const uint WriteStopSignatureNumber = 0x57545354;

        public OnStreamDataStream(Stream stream) {
            this._stream = stream;
        }

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush() {
            this._stream.Flush();
        }

        /// <inheritdoc cref="Stream.Read(byte[],int,int)"/>
        public override int Read(byte[] buffer, int offset, int count) {
            long nextOnsChunk = (this._stream.Position - (this._stream.Position % FullSectionSize)) + DataSectionSize;

            int amountRead = 0;
            while (count > amountRead) {
                if (this._stream.Position >= nextOnsChunk) {
                    this._stream.Seek(AuxSectionSize - (this._stream.Position - nextOnsChunk), SeekOrigin.Current);
                    nextOnsChunk += FullSectionSize;
                }

                int wantToReadNow = Math.Min(count - amountRead, (int)(nextOnsChunk - this._stream.Position));
                int readNow = this._stream.Read(buffer, offset + amountRead, wantToReadNow);
                amountRead += readNow;
                if (wantToReadNow != readNow)
                    break; // Didn't read full amount, we likely have hit the end.
            }

            return amountRead;
        }

        /// <inheritdoc cref="Stream.Seek"/>
        public override long Seek(long offset, SeekOrigin origin) {
            long newPosition = origin switch {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => this.Position + offset,
                SeekOrigin.End => this.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            return this._stream.Seek(AddAuxSectionsToIndex(newPosition), origin);
        }

        /// <inheritdoc cref="Stream.SetLength"/>
        public override void SetLength(long value) {
            this._stream.SetLength(value);
        }

        /// <inheritdoc cref="Stream.Write(byte[],int,int)"/>
        public override void Write(byte[] buffer, int offset, int count) {
            throw new Exception("Writing to this is unsupported.");
        }

        /// <summary>
        /// Given an index to data which has both aux and user data, this will return the index for the data stream which has the aux data removed.
        /// </summary>
        /// <param name="index">Index without aux portions.</param>
        /// <returns>Index with aux portions.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RemoveAuxSectionsFromIndex(long index)
            => ((index / FullSectionSize) * DataSectionSize)
                + Math.Min(DataSectionSize, index % FullSectionSize);

        /// <summary>
        /// Given an index to a stream which has user data and no aux data, this will return the index as if aux data were present.
        /// </summary>
        /// <param name="index">Index without aux portions.</param>
        /// <returns>Index with aux portions.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AddAuxSectionsToIndex(long index)
            => ((index / DataSectionSize) * FullSectionSize) + (index % DataSectionSize);
    }

    public static class OnStreamExtensions
    {
        /// <summary>
        /// Gets a display string for an index in a file from a <see cref="DataReader"/>'s index.
        /// </summary>
        /// <param name="reader">The reader to get the display for.</param>
        /// <returns>displayStr</returns>
        public static string GetFileIndexDisplay(this DataReader reader) {
            return reader.GetFileIndexDisplay(reader.Index);
        }
        
        /// <summary>
        /// Gets a display string for an index in a file from a <see cref="DataReader"/>'s index.
        /// </summary>
        /// <param name="reader">The reader to get the display for.</param>
        /// <param name="index">The index to display.</param>
        /// <returns>displayStr</returns>
        public static string GetFileIndexDisplay(this DataReader reader, long index) {
            return reader.BaseStream.GetFileIndexDisplay(index);
        }
        
        /// <summary>
        /// Gets a display string for an index in a file from a <see cref="Stream"/>'s index.
        /// </summary>
        /// <param name="stream">The stream to get the display for.</param>
        /// <param name="index">The index to display.</param>
        /// <returns>displayStr</returns>
        public static string GetFileIndexDisplay(this Stream stream, long index) {
            if (stream is OnStreamInterwovenStream interwovenStream) {
                return interwovenStream.FormatIndex(index);
            } else if (stream is OnStreamDataStream) {
                return $"0x{OnStreamDataStream.AddAuxSectionsToIndex(index):X}";
            } else {
                return $"0x{index:X}";
            }
        }
    }
}