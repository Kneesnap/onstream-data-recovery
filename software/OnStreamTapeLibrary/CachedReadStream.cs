using System;
using System.IO;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// This is a <see cref="Stream"/> which reads from a wrapped stream, only reading data when it is necessary, and caching it otherwise.
    /// It supports reading from streams regardless of if the input stream can seek or not.
    /// </summary>
    public class CachedReadStream : Stream
    {
        private readonly Stream _input;
        private readonly long _startPosition;
        private readonly long _length;
        private readonly uint _cachedChunkSize;
        private readonly byte[][] _bufferDataCache;
        private readonly bool[] _cachedBuffers;
        private long _streamPosition;
        private long _position;

        /// <inheritdoc cref="Stream.CanRead"/>
        public override bool CanRead => this._input.CanRead;
        /// <inheritdoc cref="Stream.CanSeek"/>
        public override bool CanSeek => true;
        /// <inheritdoc cref="Stream.CanWrite"/>
        public override bool CanWrite => false;
        /// <inheritdoc cref="Stream.Length"/>
        public override long Length => (this._length - this._startPosition);

        /// <inheritdoc cref="Stream.Position"/>
        public override long Position
        {
            get => this._position;
            set => this._position = value;
        }
        
        /// <summary>
        /// Get the total number of bytes in the last cache chunk.
        /// 0 is returned if all of the chunks are the same size.
        /// </summary>
        public uint SizeOfLastChunk => (uint) (this._length % this._cachedChunkSize);

        /// <summary>
        /// Get the total number of cache chunks.
        /// </summary>
        public uint TotalCacheChunkCount => (uint) this._cachedBuffers.LongLength;

        /// <summary>
        /// This represents the current cache chunk.
        /// </summary>
        public uint CurrentCacheChunkID
        {
            get
            {
                uint chunkID = (uint) (this._position / this._cachedChunkSize);
                if (chunkID >= this._cachedBuffers.Length - 1)
                {
                    uint lastChunkSize = this.SizeOfLastChunk;
                    if (lastChunkSize > 0) 
                        chunkID += (uint) (this._position % this._cachedChunkSize) / lastChunkSize;
                }

                return chunkID;
            }
        } 
        
        /// <summary>
        /// Creates an instance of <see cref="CachedReadStream"/>.
        /// The stream position and length will be determined from the input stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="cachedChunkSize">The size of the cache chunk arrays.</param>
        public CachedReadStream(Stream stream, uint cachedChunkSize = 1024)
            : this(stream, stream.Position, stream.Length, cachedChunkSize)
        {
        }
        
        /// <summary>
        /// Creates an instance of <see cref="CachedReadStream"/>.
        /// Some streams don't allow getting Length / Position data from them, so we allow providing the information to this constructor if necessary.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="startPosition"> The start position. Generally 0 if not getting from Stream.</param>
        /// <param name="length"> The length of the stream, since the stream probably doesn't support getting length.</param>
        /// <param name="cachedChunkSize">The size of the cache chunk arrays.</param>
        public CachedReadStream(Stream stream, long startPosition, long length, uint cachedChunkSize = 1024)
        {
            if (cachedChunkSize == 0)
                throw new Exception("Invalid cached chunk size.");
            
            this._input = stream;
            this._cachedChunkSize = cachedChunkSize;

            this._length = length;
            this._startPosition = startPosition;
            long abridgedLength = this._length - this._startPosition;
            uint fullChunkCount = (uint) (abridgedLength / cachedChunkSize);
            uint lastChunkSize = (uint) (abridgedLength % cachedChunkSize);

            uint totalChunkCount = fullChunkCount + (uint) (lastChunkSize > 0 ? 1 : 0);
            this._bufferDataCache = new byte[totalChunkCount][];
            this._cachedBuffers = new bool[totalChunkCount];
            this._streamPosition = 0;
            this._position = 0;
        }

        /// <inheritdoc cref="Stream.Flush"/>
        public override void Flush() {
            this._input.Flush();
        }

        /// <inheritdoc cref="Stream.Read(byte[],int,int)"/>
        public override int Read(byte[] buffer, int offset, int count) {
            int amountRead = 0;
            while (count > amountRead) {
                uint currentChunkID = this.CurrentCacheChunkID;

                if (currentChunkID >= this._bufferDataCache.Length)
                    break; // There's no more data to read from the stream.

                // If this isn't cached, it's time to cache it.
                if (!this._cachedBuffers[currentChunkID])
                {
                    if (this._input.CanSeek)
                    {
                        // The stream can seek, so we'll seek to the first available position.
                        this._streamPosition = currentChunkID * this._cachedChunkSize;
                        this._input.Seek(this._streamPosition + this._startPosition, SeekOrigin.Begin);
                        this.ReadNextChunk();
                    }
                    else
                    {
                        // This stream can't seek, so we just have to read and cache chunks until we get there.
                        while (!this._cachedBuffers[currentChunkID])
                            this.ReadNextChunk();
                    }
                }

                byte[] cachedData = this._bufferDataCache[currentChunkID];

                long cachePos = this._position % this._cachedChunkSize;
                int bytesRead = Math.Min(count - amountRead, cachedData.Length - (int) cachePos);
                Array.ConstrainedCopy(cachedData, (int) cachePos, buffer, offset + amountRead, bytesRead);
                amountRead += bytesRead;
                this._position += bytesRead;
            }

            return amountRead;
        }

        private void ReadNextChunk()
        {
            // Calculate the current chunk ID from where the _input stream is, NOT where this reader wants us to be.
            // This is because this function always reads the next chunk, not necessarily the desired one.
            uint currentChunkID = (uint) ((this._streamPosition - this._startPosition) / this._cachedChunkSize);

            uint sizeOfLastChunk = this.SizeOfLastChunk;
            int desiredAmount = (int) (sizeOfLastChunk > 0 && currentChunkID >= this.TotalCacheChunkCount - 1
                ? sizeOfLastChunk : this._cachedChunkSize);
            int amountRead = 0;

            byte[] cachedDataChunk = new byte[desiredAmount];
            while (desiredAmount > amountRead)
            {
                int desiredAmountNow = desiredAmount - amountRead;
                int readAmountNow = this._input.Read(cachedDataChunk, amountRead, desiredAmountNow);
                if (readAmountNow <= 0)
                    break; // Didn't read full amount, we likely have hit the end.
                        
                amountRead += readAmountNow;
                this._streamPosition += readAmountNow;
            }

            if (desiredAmount > amountRead)
                throw new Exception($"Failed to read {desiredAmount} bytes from the input stream, only {amountRead} could be read.");
            
            this._cachedBuffers[currentChunkID] = true;
            this._bufferDataCache[currentChunkID] = cachedDataChunk;
        }

        /// <inheritdoc cref="Stream.Seek"/>
        public override long Seek(long offset, SeekOrigin origin) {
            return this._position = origin switch {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => this._position + offset,
                SeekOrigin.End => this.Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
        }

        /// <inheritdoc cref="Stream.SetLength"/>
        public override void SetLength(long value) {
            if (this.Length != value)
                throw new Exception($"Changing the length of {nameof(CachedReadStream)} is unsupported.");
            
            this._input.SetLength(value);
        }

        /// <inheritdoc cref="Stream.Write(byte[],int,int)"/>
        public override void Write(byte[] buffer, int offset, int count) {
            throw new Exception($"Writing to {nameof(CachedReadStream)} is unsupported.");
        }
    }
}