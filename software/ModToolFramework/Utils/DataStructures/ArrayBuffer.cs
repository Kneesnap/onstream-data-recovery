using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ModToolFramework.Utils.DataStructures {
    /// <summary>
    /// Represents an array which gets used like a buffer.
    /// </summary>
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    [SuppressMessage("ReSharper", "ConvertToAutoPropertyWithPrivateSetter")]
    public class ArrayBuffer<TValue> : ArrayBuffer {
        private int _bufferUsedLength;
        
        /// <summary>
        /// Gets the internal array used to represent the buffer.
        /// Only use this if you really know what you're doing.
        /// This is useful for things like getting a 'ref' to an element in the array.
        /// This is also significantly more performant than using the array accessors on this class.
        /// NOTE: Internal does not indicate accessibility here, rather it indicates it's the array this class is doing work on internally.
        /// </summary>
        public TValue[] InternalArray;

        /// <summary>
        /// Gets the number of elements which are currently defined in the buffer.
        /// This is not the buffer capacity.
        /// </summary>
        public int Length => this._bufferUsedLength;

        /// <summary>
        /// Gets the current buffer capacity. (Dynamically expands.)
        /// </summary>
        public int Capacity => this.InternalArray.Length;

        /// <summary>
        /// Sets the first unused buffer position with a value.
        /// NOTE: If EnsureSize or EnsureAvailable have been used to ensure there is space for following elements,
        /// using this will skip over the space allocated for that, which may not be intuitive, but it is intentional.
        /// </summary>
        public TValue NextEmpty {
            set {
                this.EnsureAvailable(1);
                this.InternalArray[this._bufferUsedLength - 1] = value; // Need to subtract 1 because bufferUsedLength increases with the ensure available call.
            }
        }

        public TValue this[int index] {
            get {
                if (index < 0 || index >= this._bufferUsedLength)
                    throw new IndexOutOfRangeException($"Index {index} is not in the used buffer range. [0, {this._bufferUsedLength})");
                return this.InternalArray[index];
            }
            set {
                this.EnsureSize(index + 1);
                this.InternalArray[index] = value;
            }
        }
        
        public TValue this[uint index] {
            get {
                if (index >= this._bufferUsedLength)
                    throw new IndexOutOfRangeException($"Index {index} is not in the used buffer range. [0, {this._bufferUsedLength})");
                return this.InternalArray[index];
            }
            set {
                this.EnsureSize((int)(index + 1));
                this.InternalArray[index] = value;
            }
        }
        
        public ArrayBuffer(int initialSize) {
            this.InternalArray = new TValue[initialSize];
        }

        public ArrayBuffer(TValue[] buffer) {
            this.InternalArray = buffer;
        }

        /// <summary>
        /// Ensures the buffer has enough space to hold a given number of elements.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the buffer.</param>
        public void EnsureSize(uint minimumSize) {
            if (minimumSize > this._bufferUsedLength)
                this._bufferUsedLength = (int)minimumSize;
            
            if (this.InternalArray.Length > minimumSize)
                return; // Don't need to expand the buffer.
            
            // Expand buffer.
            int expandSize = this.InternalArray.Length;
            while (minimumSize > expandSize)
                expandSize *= 2;
            if (expandSize > this.InternalArray.Length)
                this.ResizeBuffer(expandSize);
        }

        /// <summary>
        /// Ensure there is room for at least a given number of elements more than what has already been stored.
        /// </summary>
        /// <param name="elementsAvailable">The number of additional elements to ensure space for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureAvailable(int elementsAvailable) {
            this.EnsureSize(this._bufferUsedLength + elementsAvailable);
        }

        /// <summary>
        /// Ensures the buffer has enough space to hold a given number of elements.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the buffer.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSize(int minimumSize) {
            if (minimumSize < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumSize), "Size must be greater than zero.");
            this.EnsureSize((uint)minimumSize);
        }

        /// <inheritdoc cref="ArrayBuffer"/>
        public virtual void Clear() {
            this._bufferUsedLength = 0;
        }

        /// <inheritdoc cref="ArrayBuffer"/>
        public void RemoveElement(int index) {
            if (index < 0 || index >= this._bufferUsedLength)
                throw new ArgumentOutOfRangeException($"The provided index ({index}) was outside of the range of the buffer! Range: [0, {this._bufferUsedLength})");
            Array.ConstrainedCopy(this.InternalArray, index + 1, this.InternalArray, index, this._bufferUsedLength - index - 1);
            this._bufferUsedLength--;
        }

        private void RemoveElements(int[] indices, int useLength) {
            if (indices == null)
                throw new ArgumentNullException(nameof(indices));

            int lastIndex = -1;
            bool isSorted = true;
            for (int i = 0; i < useLength; i++) {
                int currIndex = indices[i];
                if (currIndex < 0 || currIndex >= this._bufferUsedLength)
                    throw new ArgumentOutOfRangeException(nameof(indices), $"Index #{i} was {currIndex}, which was not inside the bounds of the buffer. Range: [0, {this._bufferUsedLength})");

                if (isSorted && lastIndex > currIndex)
                    isSorted = false;
                lastIndex = currIndex;
            }
            
            if (!isSorted)
                Array.Sort(indices, 0, useLength);
            
            // Remove Elements.
            int removedTotal = 0;
            for (int i = 0; i < useLength; i++) {
                int nextIndex = ((useLength > i + 1) ? indices[i + 1] : this._bufferUsedLength - 1) - removedTotal;
                int removeIndex = indices[i] - removedTotal; // This works because TestRemovals is sorted.
                int copyLength = (nextIndex - removeIndex);
                if (copyLength > 0) { // If copy length is 0, that means there is a duplicate index, and we can just safely skip it.
                    // I did performance testing, and this is easily the fastest (and best-scaling) method to do this.
                    Array.ConstrainedCopy(this.InternalArray, removeIndex + removedTotal + 1, this.InternalArray, removeIndex, copyLength);
                    removedTotal++;
                } else if (i == useLength - 1) { // Edge-case, if we're at the end of the array, copyLength can be zero because there would be nothing to copy. This should still be counted as a removal.
                    removedTotal++;
                }
            }

            this._bufferUsedLength -= useLength;
        }

        /// <inheritdoc cref="ArrayBuffer"/>
        public void RemoveElements(int[] indices) {
            this.RemoveElements(indices, indices.Length);
        }
        
        /// <inheritdoc cref="ArrayBuffer"/>
        public void RemoveElements(ArrayBuffer<int> indices) {
            this.RemoveElements(indices.InternalArray, indices.Length);
        }

        private protected virtual void ResizeBuffer(int newSize) {
            TValue[] newBuffer = new TValue[newSize];
            if (this.InternalArray != null) 
                Array.Copy(this.InternalArray, newBuffer, Math.Min(newSize, this.InternalArray.Length));
            this.InternalArray = newBuffer;
        }

        /// <summary>
        /// Creates an array with all of the data in this buffer in order.
        /// </summary>
        /// <returns>bufferArray</returns>
        public TValue[] ToArray() {
            TValue[] newArray = new TValue[this.Length];
            Array.ConstrainedCopy(this.InternalArray, 0, newArray, 0, this.Length);
            return newArray;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"{this.GetType().Name}{this.InternalArray.ToDisplayString(0, this._bufferUsedLength)} (Length = {this._bufferUsedLength})";
        }

        /// <inheritdoc cref="IDisposable"/>
        public virtual void Dispose() {
            if (this.InternalArray != null) {
                this.InternalArray = null;
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    /// Represents an OpenGL / OpenTK buffer.
    /// </summary>
    public interface ArrayBuffer : IDisposable {
        /// <summary>
        /// Gets the number of elements which are currently defined in the buffer.
        /// This is not the buffer capacity.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets the current buffer capacity. (Dynamically expands.)
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Ensure there is room for at least a given number of elements more than what has already been stored.
        /// </summary>
        /// <param name="elementsAvailable">The number of additional elements to ensure space for.</param>
        public void EnsureAvailable(int elementsAvailable);
        
        /// <summary>
        /// Ensures the buffer has enough space to hold a given number of elements.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the buffer.</param>
        public void EnsureSize(int minimumSize);
        
        /// <summary>
        /// Ensures the buffer has enough space to hold a given number of elements.
        /// </summary>
        /// <param name="minimumSize">The minimum size of the buffer.</param>
        public void EnsureSize(uint minimumSize);
        
        /// <summary>
        /// Acts as if the contents of the buffer had been cleared. Resets the length, etc.
        /// The values inside will not change though.
        /// </summary>
        public void Clear();
        
        /// <summary>
        /// Removes an element at the provided index.
        /// </summary>
        /// <param name="index">The index to remove the element at.</param>
        public void RemoveElement(int index);

        /// <summary>
        /// Removes elements at the indices provided.
        /// Recommended to use stackalloc for the array in performance-sensitive code.
        /// Removes all the elements with worst-case O(n) efficiency, where n is the number of elements in the array.
        /// The indices which should be passed should correspond to the values to be removed at the time of calling. Duplicate indices are ignored.
        /// NOTE: This is ALWAYS recommended over RemoveElement(int) if you have more than one index to remove, because this is magnitudes more efficient.
        /// </summary>
        /// <param name="indices">The indices to the elements which should be removed.</param>
        public void RemoveElements(int[] indices);
        
        /// <summary>
        /// Removes elements at the indices provided.
        /// Recommended to use stackalloc for the array in performance-sensitive code.
        /// Removes all the elements with worst-case O(n) efficiency, where n is the number of elements in the array.
        /// The indices which should be passed should correspond to the values to be removed at the time of calling. Duplicate indices are ignored.
        /// NOTE: This is ALWAYS recommended over RemoveElement(int) if you have more than one index to remove, because this is magnitudes more efficient.
        /// </summary>
        /// <param name="indices">The indices to the elements which should be removed.</param>
        public void RemoveElements(ArrayBuffer<int> indices);
    }
}