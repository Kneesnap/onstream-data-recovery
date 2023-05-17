using System;
using System.IO;

namespace ModToolFramework.Utils.Data
{
    /// <summary>
    /// The mode which bits should be read.
    /// </summary>
    public enum BitOrderMode
    {
        HighToLow, // Reads bits starting with the highest bit, ending with the lowest bit. (Per-byte)
        LowToHigh// Reads bits starting with the lowest bit, ending with the highest bit. (Per-byte)
    }
    
    /// <summary>
    /// Used to read individual bits from a stream.
    /// </summary>
    public class BitReader : IDisposable
    {
        private bool _hasReadingBegun;
        private BitOrderMode _mode;
        private readonly bool _closeStreamOnDispose;
        
        /// <summary>
        /// The reader which bytes are to be read from. (The bytes containing the bits to read.)
        /// The way this reader reads bits is as follows:
        ///
        /// BitReader reads a full byte from the DataReader when it needs to get more bits than it currently knows about.
        /// This byte will be cached, so even if the DataReader has more reads performed, the when ReadBit() is called, it will return a bit from the cached bit.
        /// The use-case of this functionality would be something like RNC ProPack compression.
        /// Once it reads all the bits from the cached byte, it will read the next byte from the current index of the DataReader.
        /// </summary>
        public DataReader Reader { get; private set; }
        
        /// <summary>
        /// The current byte which bits are getting read from.
        /// </summary>
        public byte? CurrentByte { get; private set; }
        
        /// <summary>
        /// The position of the current bit in the current byte.
        /// </summary>
        public byte BitPos { get; private set; }
        
        /// <summary>
        /// The mode to use for reading bits.
        /// Should not be changed after reading begins.
        /// </summary>
        public BitOrderMode Mode { get => this._mode;
            set {
                if (this._hasReadingBegun)
                    throw new InvalidOperationException($"Cannot change {nameof(BitOrderMode)} after reading has begun.");
                this._mode = value;
            }
        }

        public BitReader(DataReader reader, bool closeStreamOnDispose = true) {
            this.Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            this._closeStreamOnDispose = closeStreamOnDispose;
            this._mode = BitOrderMode.HighToLow;
        }

        private void ReadNextByte() {
            this._hasReadingBegun = true;
            this.CurrentByte = this.ReadNextByteFromStreamOrNull();
        }

        /**
         * Skips any remaining bits in the byte.
         */
        public void SkipRestOfByte() {
            if (this.BitPos == 0)
                return;
            
            this.BitPos = 0;
            this.ReadNextByte();
        }

        /// <summary>
        /// Reads the next byte from the stream.
        /// This will read the next byte and return it. The pending bits from the current byte will remain unchanged.
        /// When the reader needs to access the next bits, it will read the 8 bits from the byte following this one.
        /// Example use-case: RNC decompression.
        /// </summary>
        /// <returns>nextStreamByte. Returns null if there are no more.</returns>
        private byte? ReadNextByteFromStreamOrNull() {
            return this.Reader.HasMore ? this.Reader.ReadByte() : null;
        }

        /// <summary>
        /// Reads a bit from the stream.
        /// </summary>
        /// <returns>readBit</returns>
        public bool ReadBit() {
            if (this.CurrentByte == null) {
                this.ReadNextByte();
                if (this.CurrentByte == null) // It's still null.
                    throw new EndOfStreamException("The BitReader has no more bits to read.");
            }
            
            byte useByte = this.CurrentByte.Value;
            int readBitShift = (this._mode == BitOrderMode.LowToHigh) ? this.BitPos : (DataConstants.BitsPerByte - 1 - this.BitPos);

            if (this.BitPos == DataConstants.BitsPerByte - 1) {
                this.BitPos = 0;
                this.ReadNextByte();
            } else {
                this.BitPos++;
            }

            return ((useByte >> readBitShift) & DataConstants.BitTrue) == DataConstants.BitTrue;
        }

        /// <summary>
        /// Reads a bit from the stream, returning either zero or one.
        /// </summary>
        /// <returns>readBit</returns>
        public int ReadBitAsNumber() {
            if (this.CurrentByte == null) {
                this.ReadNextByte();
                if (this.CurrentByte == null) // It's still null.
                    throw new EndOfStreamException("The BitReader has no more bits to read.");
            }
            
            byte useByte = this.CurrentByte.Value;
            int readBitShift = (this._mode == BitOrderMode.LowToHigh) ? this.BitPos : (DataConstants.BitsPerByte - 1 - this.BitPos);

            if (this.BitPos == DataConstants.BitsPerByte - 1) {
                this.BitPos = 0;
                this.ReadNextByte();
            } else {
                this.BitPos++;
            }

            return ((useByte >> readBitShift) & DataConstants.BitTrue);
        }

        /// <summary>
        /// Read a given number of bits into a bool array.
        /// </summary>
        /// <param name="bits">The amount of bits to read.</param>
        /// <returns>readBits</returns>
        public bool[] ReadBits(int bits) {
            bool[] resultingBits = new bool[bits];
            for (int i = 0; i < bits; i++)
                resultingBits[i] = this.ReadBit();
            return resultingBits;
        }

        /// <summary>
        /// Reads bits from the stream into an integer.
        /// Be careful though, since this is affected by reading bits in reverse when you enable that.
        /// Defaults to little endian.
        /// </summary>
        /// <param name="bitCount">The number of bits to read. [1,32].</param>
        /// <param name="endian">The endian to read with. Defaults to little endian or "The first bit read will be the lowest bit in the resulting integer.""</param>
        /// <returns>loadedBitNumber</returns>
        public int ReadBitsAsInteger(int bitCount, ByteEndian endian = ByteEndian.LittleEndian) {
            if (bitCount < 0 || bitCount > 32)
                throw new ArgumentOutOfRangeException(nameof(bitCount), "bitCount was invalid, it must be between 0 and 32.");

            int result = 0;
            for (int bit = 0; bit < bitCount; bit++) {
                bool bitTrue = this.ReadBit();

                if (endian == ByteEndian.LittleEndian) {
                    if (bitTrue)
                        result |= (1 << bit);
                } else if (endian == ByteEndian.BigEndian) {
                    result <<= 1; // Shift old bits left 1 unconditionally.
                    if (bitTrue)
                        result |= DataConstants.BitTrue;
                }
            }

            return result;
        }

        /// <inheritdoc cref="DataReader.Dispose"/>
        public void Dispose() {
            if (this._closeStreamOnDispose) {
                this.Reader.Dispose();
                this.Reader = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Test if there are more bits available to read.
        /// </summary>
        /// <returns>Whether there are remaining bits.</returns>
        public bool HasMore() {
            return this.BitPos != DataConstants.BitsPerByte - 1 || this.Reader.HasMore;
        }
    }
}
