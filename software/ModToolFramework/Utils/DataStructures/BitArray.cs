using System;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// Represents an array of bits.
    /// </summary>
    public class BitArray
    {
        private readonly byte[] _array;

        /// <summary>
        /// The number of bits which can be represented in the array.
        /// </summary>
        public readonly int Count;

        public int this[int index] {
            get {
                if (index < 0 || index >= this.Count)
                    throw new IndexOutOfRangeException($"The bit {index} cannot be accessed, there are {this.Count} bits in the array.");
                return (this._array[index / DataConstants.BitsPerByte] >> (index % DataConstants.BitsPerByte)) & 1;
            }
            set => _ = this.Set(index, value != 0);
        }

        public BitArray(byte[] array) {
            this._array = array;
        }

        public BitArray(int bitCount) {
            this._array = new byte[(bitCount / DataConstants.BitsPerByte) + (bitCount % DataConstants.BitsPerByte > 0 ? 1 : 0)];
            this.Count = bitCount;
        }

        /// <summary>
        /// Gets the bit at a certain index as a boolean.
        /// </summary>
        /// <param name="index">The index to get the bit from.</param>
        /// <returns>bitState</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is not valid.</exception>
        public bool Get(int index) {
            if (index < 0 || index >= this.Count)
                throw new IndexOutOfRangeException($"The bit {index} cannot be accessed, there are {this.Count} bits in the array.");
            return ((this._array[index / DataConstants.BitsPerByte] >> (index % DataConstants.BitsPerByte)) & 1) == 1;
        }
        
        /// <summary>
        /// Sets the boolean value at the specified index.
        /// </summary>
        /// <param name="index">The index to get the bit from.</param>
        /// <param name="newState">The index to get the bit from.</param>
        /// <returns>The old state before it was set.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is not valid.</exception>
        public bool Set(int index, bool newState) {
            bool oldValue = this.Get(index);
            if (oldValue == newState)
                return oldValue;
            
            int byteIndex = (index / DataConstants.BitsPerByte);
            int bitIndex = (index % DataConstants.BitsPerByte);
            
            if (newState) {
                this._array[byteIndex] |= (byte)(1U << bitIndex);
            } else {
                this._array[byteIndex] &= (byte)~(1U << bitIndex);
            }

            return oldValue;
        }
        
        /// <summary>
        /// Flips the bit at a certain index.
        /// </summary>
        /// <param name="index">The index to get the bit from.</param>
        /// <returns>The bool state of the bit before it was flipped.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is not valid.</exception>
        public bool Flip(int index) {
            return this.Set(index, !this.Get(index));
        }
    }
}