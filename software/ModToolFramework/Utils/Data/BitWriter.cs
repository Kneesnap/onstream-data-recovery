using System;
using System.Data;
using System.IO;

namespace ModToolFramework.Utils.Data {
    /// <summary>
    /// A tool for writing raw bits (not bytes!) to a stream.
    /// WARNING: Please use #ToArray(), or #FinishCurrentByte() before accessing the underlying stream or calling Dispose. The last byte will not be written unless you do this.
    /// </summary>
    public class BitWriter : IDisposable {
        private readonly bool _closeStreamOnDispose;
        private long _reservedIndex = -1;
        private bool _hasWritingBegun;
        private BitOrderMode _mode;


        public BitWriter(DataWriter writer, bool closeStreamOnDispose = true) {
            this.Writer = writer;
            this._closeStreamOnDispose = closeStreamOnDispose;
        }

        /// <summary>
        /// The writer which bytes are to be written to. (The bytes containing the bits)
        /// When the first bit of a byte is written, the byte at the writer's index is reserved.
        /// If any data is written, it will write to the subsequent byte(s) after the reserved one.
        /// The use-case of this functionality would be something like RNC ProPack compression.
        /// Once the byte has all its bit set, it will be written to the reserved slot.
        /// Once it reads all the bits from the cached byte, it will read the next byte from the current index of the DataReader.
        ///
        /// This makes it safe to write data to the writer even during the middle of writing a bit. An example use-case of this functionality is RNC ProPack compression.
        /// </summary>
        public DataWriter Writer { get; private set; }
        
        /// <summary>
        /// The current byte which which once enough bits are set will be written.
        /// </summary>
        public byte CurrentByte { get; private set; }
        
        /// <summary>
        /// The position of the current bit in the current byte.
        /// </summary>
        public byte BitPos { get; private set; }
        
        /// <summary>
        /// The mode to use for ordering the written bits.
        /// Should not be changed after writing begins.
        /// </summary>
        public BitOrderMode Mode {
            get => this._mode;
            set {
                if (this._hasWritingBegun)
                    throw new InvalidOperationException($"Cannot change {nameof(BitOrderMode)} after reading has begun.");
                this._mode = value;
            }
        }

        /// <inheritdoc cref="DataWriter.Dispose"/>
        public void Dispose() {
            this.FinishCurrentByte();
            
            if (this._closeStreamOnDispose) {
                this.Writer.Dispose();
                this.Writer = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Writes a single bit to the stream.
        /// </summary>
        /// <param name="bit">Whether the bit is a 1 (true) or a 0 (false).</param>
        public void WriteBit(bool bit) {
            this._hasWritingBegun = true;
            
            // If this is the first bit, reserve the byte.
            if (this.BitPos == 0)
                this._reservedIndex = this.Writer.WriteNullBytes(1);

            // Writes the bit to the current byte.
            if (bit)
                this.CurrentByte |= (byte)(DataConstants.BitTrue << this.GetCurrentBitID());
            
            // Writes the byte, if it is time.
            if (++this.BitPos >= DataConstants.BitsPerByte) { // Full byte, time to write.
                if (this._reservedIndex == -1)
                    throw new DataException("The reserved index for the byte containing the bits to write is not set.");
                if (this._reservedIndex > this.Writer.Index) // This isn't necessarily an error, but I cannot think of a situation where this is intended behavior.
                    throw new DataException("The reserved index to write the bits at is located after the location where the DataWriter is writing at. This likely means something managing the writer position has gone wrong.");
                
                // Writes the current bits.
                this.Writer.JumpTemp(this._reservedIndex);
                this.Writer.Write(this.CurrentByte);
                this.Writer.JumpReturn();

                // Prepares for the next bits.
                this.BitPos = 0;
                this.CurrentByte = 0;
                this._reservedIndex = -1;
            }
        }

        /// <summary>
        /// Write a given bit several times.
        /// </summary>
        /// <param name="bit">The bit to write.</param>
        /// <param name="count">The amount of times to write it.</param>
        public void WriteBits(bool bit, int count) {
            for (int i = 0; i < count; i++)
                this.WriteBit(bit);
        }

        /// <summary>
        /// Writes a given number of bits from an integer.
        /// </summary>
        /// <param name="number">The number to write bits from.</param>
        /// <param name="bitCount">The number of bits to write.</param>
        /// <param name="reverse">The parameter which can be used to reverse the order this is saved in.</param>
        public void WriteBitsFromInteger(int number, int bitCount, bool reverse = false) {
            if (bitCount < 1 || bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount), "bitCount must be between 1 and 32.");

            if (reverse) {
                for (int i = 0; i < bitCount; i++)
                    this.WriteBit(((number >> i) & DataConstants.BitTrue) == DataConstants.BitTrue);
            } else {
                for (int i = bitCount - 1; i >= 0; i--)
                    this.WriteBit(((number >> i) & DataConstants.BitTrue) == DataConstants.BitTrue);
            }
        }

        /// <summary>
        /// Writes a byte to the stream.
        /// </summary>
        /// <param name="value">The byte to write.</param>
        /// <param name="reverse">Should the bit order be reversed? Default: false.</param>
        public void WriteByte(byte value, bool reverse = false) {
            if (reverse) {
                for (int i = 0; i < DataConstants.BitsPerByte; i++)
                    this.WriteBit(((value >> i) & DataConstants.BitTrue) == DataConstants.BitTrue);
            } else {
                for (int i = DataConstants.BitsPerByte - 1; i >= 0; i--)
                    this.WriteBit(((value >> i) & DataConstants.BitTrue) == DataConstants.BitTrue);
            }
        }

        /// <summary>
        /// Read the written stream as a byte array.
        /// This will write missing bits to the stream, so only use this once all writing is complete.
        /// </summary>
        /// <param name="startIndex">The index which points to the first byte to include from the stream in the output array. Defaults to 0.</param>
        /// <param name="extraBytesBefore">Number of bytes to use as padding before the written data. Defaults to 0.</param>
        /// <param name="extraBytesAfter">Number of bytes to use as padding after the written data. Defaults to 0.</param>
        /// <returns>byteArray</returns>
        public byte[] ToByteArray(int startIndex = 0, int extraBytesBefore = 0, int extraBytesAfter = 0) {
            const int toArrayBufferSize = 4096;

            DataWriter writer = this.Writer;
            this.FinishCurrentByte();
            
            long pastePos = extraBytesBefore;
            long bytesLeftToRead = writer.BaseStream.Length - startIndex;
            byte[] results = new byte[extraBytesBefore + bytesLeftToRead + extraBytesAfter];
            byte[] buffer = new byte[toArrayBufferSize];

            writer.JumpTemp(startIndex);
            int readBytes;
            while ((readBytes = writer.BaseStream.Read(buffer, 0, toArrayBufferSize)) > 0) {
                Array.Copy(buffer, 0, results, pastePos, readBytes);
                bytesLeftToRead -= readBytes;
                pastePos += readBytes;
            }
            
            writer.JumpReturn();
            if (bytesLeftToRead != 0)
                throw new InvalidDataException("Failed to read exact number of bytes. [" + bytesLeftToRead + " left]");

            return results;
        }

        /// <summary>
        /// Finish the current byte being written with null bytes.
        /// </summary>
        /// <returns>Number of bits written ot finish the current byte.</returns>
        public int FinishCurrentByte() {
            int bitsWritten = 0;
            while (this.BitPos > 0) {
                this.WriteBit(false); // Finish the current byte.
                bitsWritten++;
            }

            return bitsWritten;
        }

        /// <summary>
        /// Gets the current bit id to be written to.
        /// </summary>
        /// <returns>currentBitId</returns>
        public int GetCurrentBitID() {
            return (this._mode == BitOrderMode.HighToLow) ? (DataConstants.BitsPerByte - this.BitPos - 1) : this.BitPos;
        }
    }
}