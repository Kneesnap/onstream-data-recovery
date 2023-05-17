using ModToolFramework.Utils.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModToolFramework.Utils.DataStructures {
    
    /// <summary>
    /// Handles obtaining an ImmutableList in a way which reduces creation of arrays. (Therefore making it acceptable to use in memory-allocation sensitive areas such as render updates.)
    /// </summary>
    /// <typeparam name="TElement">The type of object which the underlying list holds.</typeparam>
    public class CachedImmutableList<TElement> {
        private ImmutableList<TElement> _cachedImmutableList;
        private bool _cacheInvalid = true;

        /// <summary>
        /// Invalidates the cached immutable list.
        /// </summary>
        public void Invalidate() {
            this._cacheInvalid = true;
            this._cachedImmutableList = null;
        }

        /// <summary>
        /// Gets the immutable list.
        /// </summary>
        /// <returns>immutableList</returns>
        public ImmutableList<TElement> Get(List<TElement> list) {
            if (this._cacheInvalid || (this._cachedImmutableList != null && this._cachedImmutableList.Count != list.Count)) {
                this._cachedImmutableList = list.Count > 0 ? list.ToImmutableList() : ImmutableList<TElement>.Empty;
                this._cacheInvalid = false;
            }

            return this._cachedImmutableList;
        }
    }
    
    /// <summary>
    /// Handles obtaining an ImmutableList in a way which reduces creation of arrays. (Therefore making it acceptable to use in memory-allocation sensitive areas such as render updates.)
    /// </summary>
    /// <typeparam name="TSourceElement">The type of object which the underlying list holds.</typeparam>
    /// <typeparam name="TTargetElement">The type of object which the returned list holds.</typeparam>
    public abstract class CachedImmutableList<TSourceElement, TTargetElement> {
        private ImmutableList<TTargetElement> _cachedImmutableList;
        private bool _cacheInvalid = true;

        /// <summary>
        /// Invalidates the cached immutable list.
        /// </summary>
        public void Invalidate() {
            this._cacheInvalid = true;
            this._cachedImmutableList = null;
        }

        /// <summary>
        /// Gets the immutable list.
        /// </summary>
        /// <returns>immutableList</returns>
        public ImmutableList<TTargetElement> Get(List<TSourceElement> list) {
            if (this._cacheInvalid || (this._cachedImmutableList != null && this._cachedImmutableList.Count != list.Count)) {
                this._cachedImmutableList = list.Count > 0 ? this.GenerateList(list) : ImmutableList<TTargetElement>.Empty;
                this._cacheInvalid = false;
            }

            return this._cachedImmutableList;
        }

        /// <summary>
        /// Creates the ImmutableList from the source list.
        /// </summary>
        /// <param name="list">The list to convert</param>
        /// <returns>immutableList</returns>
        protected abstract ImmutableList<TTargetElement> GenerateList(List<TSourceElement> list);
    }

    /// <summary>
    /// A class used for building immutable lists.
    /// </summary>
    /// <typeparam name="TElement">The element held by the list.</typeparam>
    public class CachedImmutableListHolder<TElement> : IEnumerable<TElement>
    {
        private List<TElement> _list;
        private readonly CachedImmutableList<TElement> _cachedImmutableList = new CachedImmutableList<TElement>();

        /// <summary>
        /// The list which can be edited freely, assuming you also invalidate the cache.
        /// </summary>
        public List<TElement> List {
            get => this._list;
            set {
                if (this._list != value) {
                    this.Invalidate();
                    this._list = value;
                }
            }
        }

        public TElement this[int index] {
            get {
                if (index < 0 || index >= this.Count)
                    throw new IndexOutOfRangeException($"Index {index} was not within the list range of {this._list.GetRangeString()}.");
                return this._list[index];
            }
            set {
                if (index < 0 || index >= this.Count)
                    throw new IndexOutOfRangeException($"Index {index} was not within the list range of {this._list.GetRangeString()}.");
                this._list[index] = value;
                this.Invalidate();
            }
        }

        /// <summary>
        /// The number of elements currently in the list.
        /// </summary>
        public int Count => this._list?.Count ?? 0;

        public CachedImmutableListHolder(int initialSize = 0) {
            this._list = initialSize > 0 ? new List<TElement>(initialSize) : new List<TElement>();
        }

        public CachedImmutableListHolder(List<TElement> inputList) {
            this._list = inputList;
        }

        /// <summary>
        /// Invalidates the immutable list, so a new one will be used next time.
        /// </summary>
        public void Invalidate() {
            this._cachedImmutableList.Invalidate();
        }

        /// <summary>
        /// Gets the immutable list.
        /// </summary>
        /// <returns>The immutable list.</returns>
        public ImmutableList<TElement> Get() {
            return this._list != null && this._list.Count > 0
                ? this._cachedImmutableList.Get(this._list) : ImmutableList<TElement>.Empty;
        }

        /// <summary>
        /// Adds an object to the end of the list.
        /// </summary>
        /// <param name="element"></param>
        public void Add(TElement element) {
            this._list.Add(element);
            this.Invalidate();
        }
        
        /// <summary>
        /// Removes the first occurrence of the specified object.
        /// </summary>
        /// <param name="element">The element to remove from the list.</param>
        /// <returns>Whether or not the object was removed.</returns>
        public bool Remove(TElement element) {
            bool success = this._list.Remove(element);
            if (success)
                this.Invalidate();
            return success;
        }
        
        /// <summary>
        /// Removes the last element in the list.
        /// </summary>
        /// <returns>The element which has been removed.</returns>
        public TElement RemoveLast() {
            TElement result = this._list.RemoveLast();
            this.Invalidate();
            return result;
        }
        
        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The index of the value to remove.</param>
        public void RemoveAt(int index) {
            this._list.RemoveAt(index);
            this.Invalidate();
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The index of the value to remove.</param>
        /// <param name="removedElement">The element which has been removed.</param>
        public void RemoveAt(int index, out TElement removedElement) {
            TElement element = this._list[index];
            this._list.RemoveAt(index);
            this.Invalidate();
            removedElement = element;
        }

        /// <summary>
        /// Clears the contents of the list.
        /// </summary>
        public void Clear() {
            if (this._list.Count > 0) {
                this._list.Clear();
                this.Invalidate();
            }
        }

        /// <summary>
        /// Determines whether or not an element is in the list.
        /// </summary>
        /// <param name="item">The element to search for.</param>
        /// <returns>Whether or not it is in the list.</returns>
        public bool Contains(TElement item) {
            return this._list.Contains(item);
        }
        
        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        public IEnumerator<TElement> GetEnumerator() {
            return this._list.GetEnumerator();
        }

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
    }
}