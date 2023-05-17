using System;
using System.Collections.Generic;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// A wrapper for lists which allows accessing list data without modification.
    /// This is a substitute for <see cref="System.Collections.Immutable.ImmutableList{TElement}"/> which requires heap allocation.
    /// Lists are often the best structure for avoiding heap allocations when enumerating because of their index accessors.
    /// But sometimes we don't want to allow lists the ability to be modified in certain places, so we can wrap the list with this to ensure that no modifications occur.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public readonly ref struct ImmutableListAccessor<TElement>
    {
        private readonly List<TElement> _underlyingList;

        /// <summary>
        /// The number of elements accessed in this list.
        /// </summary>
        public int Count => _underlyingList?.Count ?? 0;
        
        /// <summary>
        /// Allows obtaining the element at a given index.
        /// </summary>
        /// <param name="index">The index of the element to get.</param>
        public TElement this[int index] {
            get {
                // Following trick can reduce the range check by one
                if (this._underlyingList == null || index >= this._underlyingList.Count)
                    throw new IndexOutOfRangeException($"The index {index} was not in the range {(this.Count > 0 ? "[" : "(")}0, {this.Count})");
                return this._underlyingList[index];
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="ImmutableListAccessor{TElement}"/>
        /// </summary>
        /// <param name="list">The list to be wrapped. Can be null.</param>
        public ImmutableListAccessor(List<TElement> list) {
            this._underlyingList = list;
        }

        /// <summary>
        /// Tests if this list contains a particular element.
        /// </summary>
        /// <param name="value">The element to search for.</param>
        /// <returns>Whether or not it is contained</returns>
        public bool Contains(TElement value) {
            return this.IndexOf(value) != -1;
        }

        /// <summary>
        /// Gets the index at which a particular element is found at, or -1 if it is not found.
        /// </summary>
        /// <param name="value">The element to search for.</param>
        /// <returns>The index it was found at, or -1.</returns>
        public int IndexOf(TElement value) {
            for (int i = 0; i < this.Count; i++)
                if (Equals(value, this._underlyingList[i]))
                    return i;
            return -1;
        }

        /// <summary>
        /// Gets this list as an array.
        /// </summary>
        /// <returns>The new array containing the values of this list.</returns>
        public TElement[] ToArray() {
            return this._underlyingList?.ToArray() ?? Array.Empty<TElement>();
        }
        
        /// <summary>
        /// Gets a modifiable copy of this list.
        /// </summary>
        /// <returns>The new list containing the values of this list.</returns>
        public List<TElement> ToList() {
            return this._underlyingList != null ? new List<TElement>(this._underlyingList) : new List<TElement>();
        }
    }
}