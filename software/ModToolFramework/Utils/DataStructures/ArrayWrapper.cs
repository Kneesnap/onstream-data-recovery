using System;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// An extended ArrayWrapper which tracks an object that holds / owns the array.
    /// </summary>
    /// <typeparam name="TElement">The element which the array holds.</typeparam>
    /// <typeparam name="TArrayHolder">The object which holds the array.</typeparam>
    public class ArrayWrapper<TElement, TArrayHolder> : ArrayWrapper<TElement>
        where TArrayHolder : class
    {
        /// <summary>
        /// The object which owns this array.
        /// </summary>
        public readonly TArrayHolder Holder;
        
        /// <summary>
        /// A listener for handling the change of an element.
        /// </summary>
        public delegate void ElementChangeHandlerWithHolder(TArrayHolder holder, ArrayWrapper<TElement> wrapper, int index, ref TElement oldValue, ref TElement newValue);

        /// <summary>
        /// This event is fired when an element is changed.
        /// </summary>
        public new event ElementChangeHandlerWithHolder OnElementChange;
        
        /// <summary>
        /// Creates a new ArrayWrapper instance.
        /// </summary>
        /// <param name="length">The size of the array.</param>
        /// <param name="holder">The object which holds this array.</param>
        /// <exception cref="ArgumentNullException">Thrown if the holder is null.</exception>
        public ArrayWrapper(int length, TArrayHolder holder) : base(length) {
            this.Holder = holder ?? throw new ArgumentNullException(nameof(holder), "The array holder cannot be null. Use the basic ArrayWrapper instead.");
            base.OnElementChange += ((ArrayWrapper<TElement> wrapper, int index, ref TElement value, ref TElement newValue) => this.OnElementChange?.Invoke(this.Holder, wrapper, index, ref value, ref newValue));
        }

        /// <summary>
        /// Creates a new ArrayWrapper instance.
        /// </summary>
        /// <param name="array">The array to wrap.</param>
        /// <param name="holder">The object which holds this array.</param>
        /// <exception cref="ArgumentNullException">Thrown if the holder is null.</exception>
        public ArrayWrapper(TElement[] array, TArrayHolder holder) : base(array) {
            this.Holder = holder ?? throw new ArgumentNullException(nameof(holder), "The array holder cannot be null. Use the basic ArrayWrapper instead.");
            base.OnElementChange += ((ArrayWrapper<TElement> wrapper, int index, ref TElement value, ref TElement newValue) => this.OnElementChange?.Invoke(this.Holder, wrapper, index, ref value, ref newValue));
        }
    }
    
    /// <summary>
    /// This wraps around an array, providing an-interface for listening for changes to the array.
    /// </summary>
    /// <typeparam name="TElement">The type of element kept in the array.</typeparam>
    public class ArrayWrapper<TElement>
    {
        private TElement[] _array;

        /// <summary>
        /// Whether or not it is possible to change the array being tracked.
        /// </summary>
        public bool AllowChangingArray { get; init; }

        /// <summary>
        /// Whether or not resizing the array is allowed.
        /// </summary>
        public bool AllowResizing { get; init; }

        /// <summary>
        /// Whether or not the ability to change array elements should be prevented.
        /// </summary>
        public bool AllowChangingElements { get; init; } = true;

        /// <summary>
        /// If null elements are allowed to be assigned. This only affects element changes.
        /// </summary>
        public bool AllowNullElements { get; init; } = true;

        /// <summary>
        /// Creates a new ArrayWrapper instance with a specified length.
        /// </summary>
        /// <param name="length">The length of the array.</param>
        public ArrayWrapper(int length) {
            this._array = new TElement[length];
        }

        /// <summary>
        /// Creates a new ArrayWrapper instance with an existing array.
        /// </summary>
        /// <param name="array">The array to wrap.</param>
        public ArrayWrapper(TElement[] array) {
            this._array = array ?? throw new ArgumentNullException(nameof(array), "Cannot use a null underlying array.");
        }
        
        /// <summary>
        /// This wrapped array should not have its elements changed, it will not fire events.
        /// The use of this is if an ArrayWrapper is no longer to be used, and the actual array needs to be moved.
        /// The setter is safe to use, and it is safe to use for read-only access too. It's just changing elements which is bad.
        /// </summary>
        /// <returns>The array.</returns>
        public TElement[] WrappedArray {
            get => this._array;
            set {
                if (!this.AllowChangingArray)
                    throw new InvalidOperationException("Changing the wrapped (underlying) array is not enabled!");
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "Cannot set the underlying array to null.");

                TElement[] oldArray = this._array;
                this._array = value;
                this.OnArrayChange?.Invoke(this, oldArray, this._array);
            }
        }

        /// <summary>
        /// Gets the number of elements which the array can hold.
        /// </summary>
        public int Length => this._array.Length;

        /// <summary>
        /// A listener for handling the change of an element.
        /// </summary>
        public delegate void ElementChangeHandler(ArrayWrapper<TElement> wrapper, int index, ref TElement oldValue, ref TElement newValue);

        /// <summary>
        /// This event is fired when an element is changed.
        /// </summary>
        public event ElementChangeHandler OnElementChange;

        /// <summary>
        /// A listener for handling the change of the underlying array.
        /// </summary>
        public delegate void ArrayChangeHandler(ArrayWrapper<TElement> wrapper, TElement[] oldArray, TElement[] newArray);

        /// <summary>
        /// This event is fired when the underlying array is changed.
        /// </summary>
        public event ArrayChangeHandler OnArrayChange;

        /// <summary>
        /// An array accessor for accessing array elements.
        /// </summary>
        /// <param name="index">The index of the array to access.</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is not within the size of the array.</exception>
        public TElement this[int index] {
            get => this._array[index];
            set {
                if (!this.AllowChangingElements)
                    throw new InvalidOperationException("This ArrayWrapper does not permit changing elements.");
                if (!this.AllowNullElements && (value == null))
                    throw new ArgumentNullException(nameof(value), "The new element value is not allowed to be null.");
                
                TElement oldValue = this._array[index];
                this._array[index] = value;
                this.OnElementChange?.Invoke(this, index, ref oldValue, ref this._array[index]);
            }
        }

        /// <summary>
        /// Changes the size of the underlying array, while preserving contents
        /// </summary>
        /// <param name="newSize">The new size of the array.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the new size is less than zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown if resizing is not enabled.</exception>
        public void Resize(int newSize) {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException(nameof(newSize), $"New array size cannot be less than zero! (Got: {newSize})");
            if (!this.AllowResizing)
                throw new InvalidOperationException("Cannot resize the array, because resizing is not enabled for this wrapper.");

            TElement[] newArray = new TElement[newSize];
            Array.Copy(this._array, 0, newArray, 0, Math.Min(newSize, this._array.Length));

            TElement[] oldArray = this._array;
            this._array = newArray;
            this.OnArrayChange?.Invoke(this, oldArray, newArray);
        }
    }
}