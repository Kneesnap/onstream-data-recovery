using ModToolFramework.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// Represents a List which is always sorted, useful for binary searches.
    /// </summary>
    /// <typeparam name="TKey">The key which everything gets sorted by.</typeparam>
    /// <typeparam name="TValue">The type of value which is tracked by the list. Structs are supported via <see cref="StructRef{TStruct}"/> and <see cref="StructWrapper{TStruct}"/>.</typeparam>
    public class SortedList<TKey, TValue>
        where TKey : IComparable<TKey>
        where TValue : class
    {
        private readonly List<SortedListEntry> _list = new List<SortedListEntry>();
        private readonly CachedImmutableList<SortedListEntry> _cachedImmutableList = new CachedImmutableList<SortedListEntry>();
        private readonly CachedImmutableList<SortedListEntry, TValue> _cachedImmutableValueList = new EntryToValueImmutableListCache();
        private TieredListSorter _tieredListSorter;
        private Comparison<TValue> _sortTiebreaker;

        /// <summary>
        /// This is used to prevent == null checks on key types, which allocates memory.
        /// </summary>
        private static readonly bool KeyTypeIsReference = !typeof(TKey).IsValueType;

        /// <summary>
        /// The tiebreaker behavior used when the keys of each element match.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if you try to change this while the list is not empty.</exception>
        public Comparison<TValue> SortTiebreaker {
            get => this._sortTiebreaker;
            set {
                if (this._list.Count > 0)
                    throw new InvalidOperationException("The sorting tiebreaker cannot be changed while the list is not empty.");
                this._sortTiebreaker = value;
            }
        }
        
        /// <summary>
        /// The order of which entries are stored.
        /// </summary>
        public SortedListOrder EntryOrder { get; init; }

        /// <summary>
        /// Gets an ImmutableList containing all of the sorted elements this SortedList contains. Sorted by the order specified by <see cref="EntryOrder"/>.
        /// </summary>
        public ImmutableList<SortedListEntry> Elements => this._cachedImmutableList.Get(this._list);

        /// <summary>
        /// Gets an ImmutableList containing all of the sorted values this contains. Sorted by the order specified by <see cref="EntryOrder"/>.
        /// </summary>
        public ImmutableList<TValue> Values => this._cachedImmutableValueList.Get(this._list);

        /// <summary>
        /// Gets the value stored at the provided index.
        /// </summary>
        /// <param name="index">The index to get the value from.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown if an index is provided which is out of the range of what is stored.</exception>
        public TValue this[int index] { get => this._list[index].Value; }

        /// <summary>
        /// The number of elements which are currently tracked.
        /// </summary>
        public int Count => this._list.Count;

        /// <summary>
        /// Gets the lowest element in the list. Returns null if the list is empty.
        /// </summary>
        public TValue LowestElement => this._list.Count > 0 ? ((this.EntryOrder == SortedListOrder.LowestToHighest) ? this._list[0].Value : this._list[^1].Value) : null;
        
        /// <summary>
        /// Gets the highest element in the list. Returns null if the list is empty.
        /// </summary>
        public TValue HighestElement => this._list.Count > 0 ? ((this.EntryOrder == SortedListOrder.LowestToHighest) ? this._list[^1].Value : this._list[0].Value) : null;

        /// <summary>
        /// Creates a new <see cref="SortedList{TKey,TValue}"/> instance.
        /// </summary>
        /// <param name="entryOrder">The order of which entries are entered into the list.</param>
        /// <param name="sortTiebreaker">Used to sort when two keys match.</param>
        public SortedList(SortedListOrder entryOrder = SortedListOrder.LowestToHighest, Comparison<TValue> sortTiebreaker = null) {
            this._sortTiebreaker = sortTiebreaker;
            this.EntryOrder = entryOrder;
        }
        
        /// <summary>
        /// Adds a new value to the list.
        /// Requires a custom implementation of <see cref="CalculateKey"/> to generate the key.
        /// </summary>
        /// <param name="value">The value which is tracked.</param>
        /// <returns>Whether or not the key value pair was successfully added. List extensions can implement behavior to cause this to return false, this will not return false in the default implementation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided key is null.</exception>
        /// <exception cref="Exception">Other exceptions may be thrown if a scenario reaches which should never happen.</exception>
        public void Add(TValue value) => this.Add(this.CalculateKey(value), value);
        
        /// <summary>
        /// Adds a new key value pair to the list.
        /// </summary>
        /// <param name="key">The key used for sorting values.</param>
        /// <param name="value">The value which is tracked.</param>
        /// <returns>Whether or not the key value pair was successfully added. List extensions can implement behavior to cause this to return false, this will not return false in the default implementation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the provided key is null.</exception>
        /// <exception cref="Exception">Other exceptions may be thrown if a scenario reaches which should never happen.</exception>
        public bool Add(TKey key, TValue value) {
            if (KeyTypeIsReference && key == null)
                throw new ArgumentNullException(nameof(key), "The key to be added to the SortedList cannot be null!");
            
            // Notation:
            // x > y means x comes after y. (x is future to y, and y is previous to x.)
            // x == y means x and y come at the same place. (x and y are current to each other.)

            if (this._list.Count == 0 || TestComparison(this._list[^1].Key, key, ComparableResult.Future)) { // If it's empty, or (value < lastElement && highestToLowest), or (value > lastElement && lowestToHighest)
                this._list.Add(new SortedListEntry(key, value));
            } else if (TestComparison(key, this._list[0].Key, ComparableResult.Future)) { // if (value < firstElement && highestToLowest) or (value > firstElement && lowestToHighest)
                this._list.Insert(0, new SortedListEntry(key, value));
            } else {
                bool keyAlreadyExists = this.FindIndicesForKey(key, value, out int startIndex, out _, out int insertionIndex);

                if (keyAlreadyExists) { // Runs duplication checks.
                    if (!this.OnAddingDuplicateKey(key, value))
                        return false;

                    int matchCount = 0;

                    int startAt = (this.EntryOrder == SortedListOrder.HighestToLowest) ? insertionIndex : insertionIndex - 1;
                    int increment = (this.EntryOrder == SortedListOrder.LowestToHighest) ? 1 : -1;
                    int endAt = startIndex - increment;

                    for (int i = startAt; i != endAt; i -= increment) {
                        SortedListEntry entry = this._list[i];
                        if (entry.Key.GetComparableResult(key) != ComparableResult.Current)
                            break;
                        if (Equals(value, entry.Value))
                            matchCount++;
                    }

                    if (matchCount > 0 && !this.OnAddingDuplicateValue(key, value, matchCount))
                        return false; // Test if duplicate key value pairs are allowed.
                }
                
                this._list.Insert(insertionIndex, new SortedListEntry(key, value));
            }

            this._cachedImmutableList.Invalidate();
            this._cachedImmutableValueList.Invalidate();
            return true;
        }

        /// <summary>
        /// Hook to ensure that it is allowed to have two different values which share a key.
        /// </summary>
        /// <param name="key">The key which is shared with another value.</param>
        /// <param name="value">The value to test. Note: This may actually match an existing value, but that is not something which needs to be tested here, a future check is for that.</param>
        /// <returns>Can add the value.</returns>
        protected virtual bool OnAddingDuplicateKey(TKey key, TValue value) {
            return true;
        }

        /// <summary>
        /// Hook to ensure that it is allowed to add a key value pair which perfectly matches an existing key value pair.
        /// </summary>
        /// <param name="key">The key which is shared with another key value pair.</param>
        /// <param name="value">The value which is shared with the other key value pair.</param>
        /// <param name="matchCount">The number of matches with existing entries.</param>
        /// <returns>onAddingDuplicateValue</returns>
        protected virtual bool OnAddingDuplicateValue(TKey key, TValue value, int matchCount) {
            return true;
        }

        /// <summary>
        /// Calculates the key associated with a <see cref="TValue"/>.
        /// This method MUST be overriden for it to be usable, an exception will be thrown otherwise.
        /// </summary>
        /// <param name="value">The value to calculate the key from.</param>
        /// <returns>calculatedKey</returns>
        protected virtual TKey CalculateKey(TValue value) {
            throw new InvalidOperationException($"A custom implementation of {nameof(this.CalculateKey)} must be created for {GeneralUtils.GetCallingMethodName()} to work properly. Try creating one!");
        }
        
        /// <summary>
        /// Updates the list so any keys which have changed are re-sorted.
        /// Requires a custom implementation of <see cref="CalculateKey"/>.
        /// </summary>
        public void Update() {
            for (int i = 0; i < this._list.Count; i++)
                this._list[i].Key = this.CalculateKey(this._list[i].Value);
            this._list.Sort(this._tieredListSorter ??= new TieredListSorter(this));
            this._cachedImmutableList.Invalidate();
            this._cachedImmutableValueList.Invalidate();
        }

        /// <summary>
        /// Clears all entries.
        /// </summary>
        public void Clear() {
            bool shouldInvalidate = (this._list.Count > 0);
            this._list.Clear();

            if (shouldInvalidate) {
                this._cachedImmutableList.Invalidate();
                this._cachedImmutableValueList.Invalidate();
            }
        }

        /// <summary>
        /// Removes and returns the lowest value from the list.
        /// </summary>
        /// <returns>removedValue</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the list is empty.</exception>
        public TValue RemoveLowest() {
            if (this._list.Count == 0)
                throw new IndexOutOfRangeException("Cannot remove lowest element because the list is empty.");

            this._cachedImmutableList.Invalidate();
            this._cachedImmutableValueList.Invalidate();
            return this.EntryOrder switch {
                SortedListOrder.LowestToHighest => this._list.RemoveFirst().Value,
                SortedListOrder.HighestToLowest => this._list.RemoveLast().Value,
                _ => throw new InvalidOperationException($"No behavior exists in {nameof(this.RemoveLowest)} to handle {this.EntryOrder.CalculateName()}")
            };
        }
        
        /// <summary>
        /// Removes and returns the highest value from the list.
        /// </summary>
        /// <returns>removedValue</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the list is empty.</exception>
        public TValue RemoveHighest() {
            if (this._list.Count == 0)
                throw new IndexOutOfRangeException("Cannot remove lowest element because the list is empty.");

            this._cachedImmutableList.Invalidate();
            this._cachedImmutableValueList.Invalidate();
            return this.EntryOrder switch {
                SortedListOrder.LowestToHighest => this._list.RemoveLast().Value,
                SortedListOrder.HighestToLowest => this._list.RemoveFirst().Value,
                _ => throw new InvalidOperationException($"No behavior exists in {nameof(this.RemoveHighest)} to handle {this.EntryOrder.CalculateName()}")
            };
        }

        /// <summary>
        /// Gets the key which "holds" a particular key. Every entry "holds" all keys between itself and the next entry.
        /// </summary>
        /// <param name="key">The key which is being held.</param>
        /// <param name="holder">The output which contains the key that is holding the input key.</param>
        /// <param name="holdingOrder">Specifies whether keys "hold" the areas below them or above them. Defaults to lowest to highest, which means they control the values above them until the next entry.</param>
        /// <returns>Whether or not the key was actually held by anything.</returns>
        public bool GetHoldingKey(TKey key, out TKey holder, SortedListOrder holdingOrder = SortedListOrder.LowestToHighest) {
            if (KeyTypeIsReference && key == null)
                throw new ArgumentNullException(nameof(key));

            if (this.FindIndicesForKey(key, null, out _, out _, out int insertionIndex)) {
                // The key was found, and because a key always is considered its own holder, return itself.
                holder = key;
                return true;
            }

            // entry   holdOrder  offset   
            // LO->HI    LO->HI  -1
            // LO->HI    HI->LO  0
            // HI->LO    LO->HI  0
            // HI->LO    HI->LO  -1
            int offset = (this.EntryOrder == holdingOrder) ? -1 : 0;
            int holderIndex = insertionIndex + offset;
            if (holderIndex < 0 || holderIndex >= this._list.Count) {
                // If the holder index isn't within the list, then it must not have a holder.
                holder = default;
                return false;
            }
            
            holder = this._list[holderIndex].Key;
            return true;
        }

        /// <summary>
        /// Checks if there are any keys between the first and second keys (inclusive).
        /// </summary>
        /// <param name="firstKey">The key to start the search at.</param>
        /// <param name="secondKey">The key to stop the search at.</param>
        /// <returns>If there are any keys between these two.</returns>
        public bool HasAnyKeysBetween(TKey firstKey, TKey secondKey) { // In the future, once we internally store things as List<List<TValue>>, this will be much simpler, because it will be allowed to just check the indices.
            if (KeyTypeIsReference && firstKey == null)
                throw new ArgumentNullException(nameof(firstKey));
            if (KeyTypeIsReference && secondKey == null)
                throw new ArgumentNullException(nameof(secondKey));
            
            if (this._list.Count == 0)
                return false;
            
            // Using yield is a more memory efficient way of handling this than creating a list or anything else, according to https://stackoverflow.com/questions/29702468/c-sharp-yield-return-performance

            bool isHighToLow = (this.EntryOrder == SortedListOrder.HighestToLowest);
            int increment = isHighToLow ? -1 : 1;

            if (!this.FindIndicesForKey(firstKey, null, out int startIndex, out _, out int firstKeyInsertionIndex)) {
                startIndex = firstKeyInsertionIndex - (isHighToLow ? 1 : 0);
                if (startIndex == -1) // If it's negative one, it means we've wrapped around but didn't find anything, so we should go back as to avoid including keys which aren't included.
                    startIndex = this._list.Count - 1;
                if (startIndex == this._list.Count)
                    startIndex = 0;
            }

            if (!this.FindIndicesForKey(secondKey, null, out _, out int endIndex, out int secondKeyInsertionIndex)) {
                endIndex = secondKeyInsertionIndex - (isHighToLow ? 0 : 1);
                if (endIndex == -1) // If it's negative one, it means we've wrapped around but didn't find anything, so we should go back as to avoid including keys which aren't included.
                    endIndex = this._list.Count - 1;
                if (endIndex == this._list.Count)
                    endIndex = 0;
            }

            int i = startIndex;
            TKey lastSeenKey = default;
            while (true) {
                TKey currentKey = this._list[i].Key;
                
                if ((i == startIndex) || currentKey.GetComparableResult(lastSeenKey) != ComparableResult.Current)
                    return true;

                if (i == endIndex)
                    return false; // Reached end.

                if (isHighToLow && i == 0) {
                    i = this._list.Count - 1;
                } else if (!isHighToLow && i >= this._list.Count - 1) {
                    i = 0;
                } else {
                    i += increment;
                }
            }
        }

        /// <summary>
        /// Gets all keys between the first key and the second key (inclusive).
        /// If the second key is located before the first key, then the search will wrap around.
        /// The order returned is based on sequentially moving forward until the secondKey is reached.
        /// Because this is an Enumerator, changes 
        /// </summary>
        /// <param name="firstKey">The key to start the search at.</param>
        /// <param name="secondKey">The key to stop the search at.</param>
        /// <returns>keyEnumerator</returns>
        public IEnumerator<TKey> GetAllKeysBetween(TKey firstKey, TKey secondKey) {
            if (KeyTypeIsReference && firstKey == null)
                throw new ArgumentNullException(nameof(firstKey));
            if (KeyTypeIsReference && secondKey == null)
                throw new ArgumentNullException(nameof(secondKey));
            
            // Using yield is a more memory efficient way of handling this than creating a list or anything else, according to https://stackoverflow.com/questions/29702468/c-sharp-yield-return-performance

            bool isHighToLow = (this.EntryOrder == SortedListOrder.HighestToLowest);
            int increment = isHighToLow ? -1 : 1;

            if (!this.FindIndicesForKey(firstKey, null, out int startIndex, out _, out int firstKeyInsertionIndex)) {
                startIndex = firstKeyInsertionIndex - (isHighToLow ? 1 : 0);
                if (startIndex == -1) // If it's negative one, it means we've wrapped around but didn't find anything, so we should go back as to avoid including keys which aren't included.
                    startIndex = this._list.Count - 1;
                if (startIndex == this._list.Count)
                    startIndex = 0;
            }

            if (!this.FindIndicesForKey(secondKey, null, out _, out int endIndex, out int secondKeyInsertionIndex)) {
                endIndex = secondKeyInsertionIndex - (isHighToLow ? 0 : 1);
                if (endIndex == -1) // If it's negative one, it means we've wrapped around but didn't find anything, so we should go back as to avoid including keys which aren't included.
                    endIndex = this._list.Count - 1;
                if (endIndex == this._list.Count)
                    endIndex = 0;
            }

            int i = startIndex;
            TKey lastSeenKey = default;
            while (true) {
                TKey currentKey = this._list[i].Key;
                
                if ((i == startIndex) || currentKey.GetComparableResult(lastSeenKey) != ComparableResult.Current) {
                    yield return currentKey;
                    lastSeenKey = currentKey;
                }

                if (i == endIndex)
                    break; // Reached end.

                if (isHighToLow && i == 0) {
                    i = this._list.Count - 1;
                } else if (!isHighToLow && i >= this._list.Count - 1) {
                    i = 0;
                } else {
                    i += increment;
                }
            }
        }

        /// <summary>
        /// Gets the first value in the list for a particular key.
        /// Returns true / false based on if there is a value for the particular key.
        /// </summary>
        /// <param name="key">The key to get the first value for.</param>
        /// <param name="value">The output storage for the value.</param>
        /// <param name="throwErrorIfMoreThanOne">If there is more than one value for the given key, an error will be thrown if this is set to true. If this is false, the first value will be returned.</param>
        /// <returns>firstValueForKey</returns>
        public bool GetSingleValue(TKey key, out TValue value, bool throwErrorIfMoreThanOne = true) {
            return this.GetSingleValue(key, out value, out _, throwErrorIfMoreThanOne);
        }

        /// <summary>
        /// Gets the first value in the list for a particular key.
        /// Returns true / false based on if there is a value for the particular key.
        /// </summary>
        /// <param name="key">The key to get the first value for.</param>
        /// <param name="value">The output storage for the value.</param>
        /// <param name="index">The output storage for the index that the value is located in.</param>
        /// <param name="throwErrorIfMoreThanOne">If there is more than one value for the given key, an error will be thrown if this is set to true. If this is false, the first value will be returned.</param>
        /// <returns>firstValueForKey</returns>
        public bool GetSingleValue(TKey key, out TValue value, out int index, bool throwErrorIfMoreThanOne = true) {
            if (!this.FindIndicesForKey(key, null, out int startIndex, out int endIndex, out _)) {
                value = null;
                index = -1;
                return false; // Key not present.
            }

            if (throwErrorIfMoreThanOne && startIndex != endIndex)
                throw new InvalidOperationException($"There are {(startIndex - endIndex + 1)} values for the specified key, so {nameof(GetSingleValue)} cannot return a single value.");
            
            value = this._list[startIndex].Value;
            index = startIndex;
            return true;
        }

        /// <summary>
        /// Gets all of the values stored by a particular key. Returns null if there are none.
        /// </summary>
        /// <param name="key">The key to get values for.</param>
        /// <returns>values</returns>
        public List<TValue> GetValues(TKey key) {
            if (!this.FindIndicesForKey(key, null, out int startIndex, out int endIndex, out _))
                return null;

            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);
            List<TValue> values = new List<TValue>((maxIndex - minIndex) + 1);
            for (int i = minIndex; i <= maxIndex; i++)
                values.Add(this._list[i].Value);

            if (this.EntryOrder == SortedListOrder.HighestToLowest)
                values.Reverse();

            return values;
        }

        /// <summary>
        /// Get the number of values tracked for a particular key.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>Number of values stored for the particular key.</returns>
        public int GetNumberOfValuesForKey(TKey key) {
            return this.FindIndicesForKey(key, null, out int startIndex, out int endIndex, out _)
                ? (Math.Max(startIndex, endIndex) - Math.Min(startIndex, endIndex) + 1)
                : 0;
        }

        /// <summary>
        /// Remove all key value pairs which match both the supplied parameters.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns>The number of key value pairs which were removed.</returns>
        public int Remove(TKey key, TValue value) {
            if (!this.FindIndicesForKey(key, value, out int startIndex, out int endIndex, out _))
                return 0;
            
            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);
            int numberOfRemovedEntries = 0;
            
            // The removal here is not as efficient as what we do with ArrayBuffer, and stuff like that, but the expected amount of duplicated entries in this is extremely low, so it is probably okay.
            for (int i = minIndex; i <= maxIndex; i++)
                if (Equals(value, this._list[i - numberOfRemovedEntries].Value))
                    this._list.RemoveAt(i - numberOfRemovedEntries++);

            if (numberOfRemovedEntries > 0) {
                this._cachedImmutableList.Invalidate();
                this._cachedImmutableValueList.Invalidate();
            }

            return numberOfRemovedEntries;
        }
        
        /// <summary>
        /// Remove all key value pairs which use a specified key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>The number of key value pairs which were removed.</returns>
        public int Remove(TKey key) {
            if (!this.FindIndicesForKey(key, null, out int startIndex, out int endIndex, out _))
                return 0;
            
            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);
            int removeCount = (maxIndex - minIndex + 1);

            this._list.RemoveRange(minIndex, removeCount);
            if (removeCount > 0) {
                this._cachedImmutableList.Invalidate();
                this._cachedImmutableValueList.Invalidate();
            }

            return removeCount;
        }
        
        /// <summary>
        /// Removes the tracked entries which use the supplied value.
        /// Requires the keys be up to date, and CalculateKey be implemented.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <returns>Whether or not this was removed.</returns>
        public bool Remove(TValue value) {
            return this.Remove(this.CalculateKey(value), value) > 0;
        }

        /// <summary>
        /// Removes the tracked entries which use the supplied value.
        /// Requires the keys be up to date, and CalculateKey be implemented.
        /// </summary>
        /// <param name="value">The value to remove.</param>
        /// <param name="removedCount">The number of removed values.</param>
        /// <returns>Whether or not this was removed.</returns>
        public bool Remove(TValue value, out int removedCount) {
            return (removedCount = this.Remove(this.CalculateKey(value), value)) > 0;
        }

        /// <summary>
        /// Checks if a value has any tracked values.
        /// NOTE: CalculateKey must be implemented.
        /// </summary>
        /// <param name="value">The value to find.</param>
        /// <returns>Is the specified value tracked?</returns>
        public bool Contains(TValue value) {
            return this.Contains(this.CalculateKey(value));
        }
        
        /// <summary>
        /// Checks if a key has any tracked values.
        /// </summary>
        /// <param name="key">The key to find.</param>
        /// <returns>Does the key have any tracked values.</returns>
        public bool Contains(TKey key) {
            return this.FindIndicesForKey(key, null, out _, out _, out _);
        }
        
        /// <summary>
        /// Check if a certain key value pair is tracked.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="value">The value to search for.</param>
        /// <returns>If the key value pair is tracked.</returns>
        public bool Contains(TKey key, TValue value) {
            if (!this.FindIndicesForKey(key, value, out int startIndex, out int endIndex, out _))
                return false;
            
            int minIndex = Math.Min(startIndex, endIndex);
            int maxIndex = Math.Max(startIndex, endIndex);
            
            // The removal here is not as efficient as what we do with ArrayBuffer, and stuff like that, but the expected amount of duplicated entries in this is extremely low, so it is probably okay.
            for (int i = minIndex; i <= maxIndex; i++)
                if (Equals(value, this._list[i].Value))
                    return true;

            return false;
        }

        private ComparableResult GetTiebreakerComparison(TValue x, TValue y) {
            ComparableResult result = GeneralUtils.GetComparableResult(this._sortTiebreaker.Invoke(x, y)); // Future means b > a. Previous means a > b.
            return (this.EntryOrder == SortedListOrder.HighestToLowest) ? result.Invert() : result;
        }
        
        private ComparableResult GetComparison(TKey anchor, TKey compare) {
            ComparableResult result = anchor.GetComparableResult(compare); // Future means b > a. Previous means a > b.
            return (this.EntryOrder == SortedListOrder.HighestToLowest) ? result.Invert() : result;
        }
        
        private bool TestComparison(TKey anchor, TKey compare, ComparableResult lowToHighResult) {
            return this.GetComparison(anchor, compare) == lowToHighResult;
        }

        private bool FindIndicesForKey(TKey key, TValue value, out int startIndex, out int endIndex, out int insertionIndex) { // If true, it returns the start / end indices. If false, it only returns the insertion index.
            if (KeyTypeIsReference && key == null)
                throw new ArgumentNullException(nameof(key), "The key to find indices for cannot be null!");
            
            // Notation:
            // x > y means x comes after y. (x is future to y, and y is previous to x.)
            // x == y means x and y come at the same place. (x and y are current to each other.)

            int foundMidPoint = -1;
            int left = 0, lastLeft = left;
            int right = this._list.Count - 1, lastRight = right;
            while (left <= right) {
                int mid = (left + right) / 2;
                SortedListEntry midEntry = this._list[mid];

                ComparableResult comparableResult = GetComparison(midEntry.Key, key);

                if (comparableResult == ComparableResult.Current) { // We've hit a match.
                    foundMidPoint = mid;
                    break;
                } else if (comparableResult == ComparableResult.Previous) { // (value < mid && lowestToHighest) or (value > mid && highestToLowest)
                    lastRight = right;
                    right = mid - 1;
                } else if (comparableResult == ComparableResult.Future) { // (value > mid && lowestToHighest) or (value < mid && highestToLowest)
                    lastLeft = left;
                    left = mid + 1;
                }
            }

            if (foundMidPoint == -1) { // The mid point was not found.
                startIndex = int.MinValue;
                endIndex = int.MinValue; // These are not defined.
                insertionIndex = left; // I think this is right. We might need to change it slightly though.
                return false;
            }
            
            // Determine start / end indices.
            this.FindRangeOfValuesForKey(lastLeft, lastRight, foundMidPoint, key, out int furthestLeft, out int furthestRight);
            startIndex = (this.EntryOrder == SortedListOrder.HighestToLowest) ? furthestRight : furthestLeft;
            endIndex = (this.EntryOrder == SortedListOrder.HighestToLowest) ? furthestLeft : furthestRight;
            insertionIndex = this.FindInsertionIndex(value, endIndex, furthestLeft, furthestRight);
            return true;
        }

        private void FindRangeOfValuesForKey(int lastLeft, int lastRight, int someCorrectIndex, TKey key, out int furthestLeft, out int furthestRight) {
            int left = lastLeft; // Last left is acceptable, because lastLeft is always updated to be at the midpoint and since midpoints are tested, you know it must be a boundary.
            int right = someCorrectIndex;
            furthestLeft = someCorrectIndex;
            while (left <= right) {
                int mid = (left + right) / 2;
                SortedListEntry midEntry = this._list[mid];
                if (TestComparison(midEntry.Key, key, ComparableResult.Current)) {
                    furthestLeft = mid;
                    right = mid - 1;
                } else {
                    left = mid + 1;
                }
            }
            
            // Determine right-most index.
            left = someCorrectIndex;
            right = lastRight; // Last right is acceptable, because lastRight is always updated to be at the midpoint, and since midpoints are tested, you know it must be a boundary.
            furthestRight = someCorrectIndex;
            while (left <= right) {
                int mid = (left + right) / 2;
                SortedListEntry midEntry = this._list[mid];
                if (TestComparison(midEntry.Key, key, ComparableResult.Current)) {
                    furthestRight = mid;
                    left = mid + 1;
                } else {
                    right = mid - 1;
                }
            }
        }

        private int FindInsertionIndex(TValue value, int endIndex, int furthestLeft, int furthestRight) {
            if (value != null && this._sortTiebreaker != null) {
                int left = furthestLeft;
                int right = furthestRight;
                while (left <= right) {
                    int mid = (left + right) / 2;
                    SortedListEntry midEntry = this._list[mid];
                    ComparableResult comparableResult = this.GetTiebreakerComparison(value, midEntry.Value);

                    if (comparableResult == ComparableResult.Current) {
                        if (this.EntryOrder == SortedListOrder.HighestToLowest) {
                            right = mid;
                        } else {
                            left = mid;
                        }
                        
                        if (left == right || left + 1 == right)
                            break; // If left and right match after we find a correct option, there's our answer.
                        
                    } else if (comparableResult == ComparableResult.Future) { // mid > key
                        right = mid - 1;
                    } else if (comparableResult == ComparableResult.Previous) { // key < mid
                        left = mid + 1;
                    }
                }

                return left;
            } else { // There's no tiebreaker, so just add it to the end, preserving the order in which they were added.
                return endIndex + (this.EntryOrder == SortedListOrder.LowestToHighest ? 1 : 0);
            }
        }

        private class TieredListSorter : IComparer<SortedListEntry> {
            private readonly SortedList<TKey, TValue> _sortedList;

            public TieredListSorter(SortedList<TKey, TValue> sortedList) {
                this._sortedList = sortedList ?? throw new ArgumentNullException(nameof(sortedList));
            }
            
            /// <inheritdoc cref="IComparer{T}.Compare"/>
            public int Compare(SortedListEntry x, SortedListEntry y) {
                if (x == null)
                    throw new ArgumentNullException(nameof(x));
                if (y == null)
                    throw new ArgumentNullException(nameof(y));

                ComparableResult firstResult = this._sortedList.GetComparison(y.Key, x.Key);
                if (firstResult != ComparableResult.Current || this._sortedList._sortTiebreaker == null || x.Value == null || y.Value == null)
                    return firstResult.GetAsNumber<ComparableResult, int>();

                return this._sortedList.GetTiebreakerComparison(y.Value, x.Value).GetAsNumber<ComparableResult, int>();
            }
        }

        /// <summary>
        /// Represents an entry in a sorted list.
        /// </summary>
        public class SortedListEntry {
            /// <summary>
            /// The key which is used for sorting.
            /// </summary>
            public TKey Key { get; internal set; }
            
            /// <summary>
            /// The value which is tracked.
            /// </summary>
            public TValue Value { get; init; }
            
            public SortedListEntry(TKey key, TValue value) {
                this.Key = key;
                this.Value = value;
            }
        }

        private class EntryToValueImmutableListCache : CachedImmutableList<SortedListEntry, TValue>
        {
            /// <inheritdoc cref="CachedImmutableList{TSourceElement,TTargetElement}.GenerateList"/>
            protected override ImmutableList<TValue> GenerateList(List<SortedListEntry> list) {
                List<TValue> values = new List<TValue>(list.Count);
                for (int i = 0; i < list.Count; i++)
                    values.Add(list[i].Value);
                return values.ToImmutableList();
            }
        }
    }

    /// <summary>
    /// Represents the order of the sorted list.
    /// </summary>
    public enum SortedListOrder
    {
        LowestToHighest,
        HighestToLowest
    }
    
}