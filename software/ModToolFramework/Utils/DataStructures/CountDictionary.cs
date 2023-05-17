using System;
using System.Collections.Generic;

namespace ModToolFramework.Utils.DataStructures
{
    /// <summary>
    /// This dictionary tracks a count value for a value.
    /// </summary>
    /// <typeparam name="TValue">The type of value tracked.</typeparam>
    public class CountDictionary<TValue>
    {
        private readonly Dictionary<TValue, int> _countDictionary = new Dictionary<TValue, int>();
        private readonly bool _isNullAllowed;
        private int _nullCount;
        
        /// <summary>
        /// Whether or not the type this dictionary supports can be null.
        /// </summary>
        public static readonly bool CanBeNull = typeof(TValue).IsClass;

        /// <summary>
        /// Gets the values tracked by this dictionary. Will not include the null value.
        /// </summary>
        public Dictionary<TValue, int>.KeyCollection Values => this._countDictionary.Keys;
        
        /// <summary>
        /// The number of values (including null) tracked by this.
        /// </summary>
        public int Count => (this._nullCount != 0 ? 1 : 0) + this._countDictionary.Count;

        /// <summary>
        /// Creates a new instance of <see cref="CountDictionary{TValue}"/>.
        /// </summary>
        /// <param name="areNullValuesAllowed">Whether or not this instance will allow the 'null' value to be set. Defaults to false.</param>
        public CountDictionary(bool areNullValuesAllowed = false) {
            this._isNullAllowed = areNullValuesAllowed;
        }
        
        /// <summary>
        /// Gets the count tracked for the particular value.
        /// </summary>
        /// <param name="value">The value whose count we want to get.</param>
        /// <returns>The count of the particular value.</returns>
        public int GetCount(TValue value) {
            if (CanBeNull && value == null)
                return this._nullCount;

            return this._countDictionary.TryGetValue(value, out int oldCount) ? oldCount : 0;
        }
        
        /// <summary>
        /// Adds a value to the count of a particular value.
        /// Returns true if the value was not tracked before (had a count of zero), but is tracked now.
        /// </summary>
        /// <param name="value">The value to add a count to.</param>
        /// <param name="count">The count to add.</param>
        /// <returns>The operation which was performed.</returns>
        public bool AddCreateEntry(TValue value, int count = 1) {
            return this.Add(value, out _, count) == CountDictionaryOperation.AddedNewEntry;
        }
        
        /// <summary>
        /// Adds a value to the count of a particular value.
        /// </summary>
        /// <param name="value">The value to add a count to.</param>
        /// <param name="count">The count to add.</param>
        /// <returns>The operation which was performed.</returns>
        public CountDictionaryOperation Add(TValue value, int count = 1) {
            return this.Add(value, out _, count);
        }

        /// <summary>
        /// Adds a value to the count of a particular value.
        /// </summary>
        /// <param name="value">The value to add a count to.</param>
        /// <param name="newCount">The output storage for the new count after addition.</param>
        /// <param name="count">The count to add.</param>
        /// <returns>The operation which was performed.</returns>
        public CountDictionaryOperation Add(TValue value, out int newCount, int count = 1) {
            if (CanBeNull && value == null) {
                if (!this._isNullAllowed)
                    throw new InvalidOperationException($"This {this.GetTypeDisplayName()} is configured to not allow null values to be used, yet a null value was supplied to the {nameof(this.Add)} function.");

                if (count == 0) {
                    newCount = this._nullCount;
                    return CountDictionaryOperation.None;
                }

                int oldNullCount = this._nullCount;
                this._nullCount = newCount = oldNullCount + count;

                if (newCount == 0) {
                    return CountDictionaryOperation.RemovedExistingEntry;
                } else {
                    return (oldNullCount == 0) ? CountDictionaryOperation.AddedNewEntry : CountDictionaryOperation.UpdatedExistingEntry;
                }
            }

            // Get existing count.
            CountDictionaryOperation result;
            if (!this._countDictionary.TryGetValue(value, out int oldCount)) {
                oldCount = 0;
                result = CountDictionaryOperation.AddedNewEntry;
            } else {
                result = CountDictionaryOperation.UpdatedExistingEntry;
            }

            // No change, since the count is zero.
            if (count == 0) {
                newCount = oldCount;
                return CountDictionaryOperation.None;
            }

            // Apply change.
            newCount = oldCount + count;
            this._countDictionary[value] = newCount;
            if (newCount == 0) {
                this._countDictionary.Remove(value);
                result = CountDictionaryOperation.RemovedExistingEntry;
            }

            return result;
        }
        
        /// <summary>
        /// Subtracts a value from the count of a particular value.
        /// </summary>
        /// <param name="value">The value to subtract a count from.</param>
        /// <param name="count">The count to subtract.</param>
        /// <returns>The operation which was performed.</returns>
        public CountDictionaryOperation Subtract(TValue value, int count = 1) {
            return this.Subtract(value, out _, count);
        }

        /// <summary>
        /// Subtracts a value from the count of a particular value.
        /// </summary>
        /// <param name="value">The value to subtract a count from.</param>
        /// <param name="newCount">The output storage for the new count after subtraction.</param>
        /// <param name="count">The count to subtract.</param>
        /// <returns>The operation which was performed.</returns>
        public CountDictionaryOperation Subtract(TValue value, out int newCount, int count = 1) {
            if (CanBeNull && value == null) {
                if (!this._isNullAllowed)
                    throw new InvalidOperationException($"This {this.GetTypeDisplayName()} is configured to not allow null values to be used, yet a null value was supplied to the {nameof(this.Subtract)} function.");
            
                if (count == 0) {
                    newCount = this._nullCount;
                    return CountDictionaryOperation.None;
                }

                int oldNullCount = this._nullCount;
                this._nullCount = newCount = oldNullCount - count;

                if (newCount == 0) {
                    return CountDictionaryOperation.RemovedExistingEntry;
                } else {
                    return (oldNullCount == 0) ? CountDictionaryOperation.AddedNewEntry : CountDictionaryOperation.UpdatedExistingEntry;
                }
            }
            
            // Get existing count.
            CountDictionaryOperation result;
            if (!this._countDictionary.TryGetValue(value, out int oldCount)) {
                oldCount = 0;
                result = CountDictionaryOperation.AddedNewEntry;
            } else {
                result = CountDictionaryOperation.UpdatedExistingEntry;
            }
            
            // No change, since the count is zero.
            if (count == 0) {
                newCount = oldCount;
                return CountDictionaryOperation.None;
            }

            // Apply change.
            newCount = oldCount - count;
            this._countDictionary[value] = newCount;
            if (newCount == 0) {
                this._countDictionary.Remove(value);
                result = CountDictionaryOperation.RemovedExistingEntry;
            }

            return result;
        }
        
        /// <summary>
        /// Subtracts a value from the dictionary, and returns true if the subtraction succeeded.
        /// Assumes the count in the dictionary is >= 0.
        /// </summary>
        /// <param name="value">The value to apply the change to.</param>
        /// <param name="count">The count to subtract. Must not be negative.</param>
        /// <returns>Whether or not the value was removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current state does not permit this method to run.</exception>
        public bool SubtractIfPresent(TValue value, int count = 1) {
            return this.SubtractIfPresent(value, out _, count);
        }

        /// <summary>
        /// Subtracts a value from the dictionary, and returns true if the subtraction succeeded.
        /// Assumes the count in the dictionary is >= 0.
        /// </summary>
        /// <param name="value">The value to apply the change to.</param>
        /// <param name="newCount">The new count after the subtraction occurs. Must not be negative.</param>
        /// <param name="count">The count to subtract.</param>
        /// <returns>Whether or not the value was removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current state does not permit this method to run.</exception>
        public bool SubtractIfPresent(TValue value, out int newCount, int count = 1) {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), $"The count to subtract cannot be negative. ({count})");
        
            int oldCount = this.GetCount(value);
            if (oldCount < 0)
                throw new InvalidOperationException($"The method {nameof(this.SubtractIfPresent)} assumes the count of value is always >= zero. (Got {oldCount} for {value})");
            if (oldCount == 0 || count == 0) {
                newCount = 0;
                return false;
            }

            CountDictionaryOperation operation = this.Subtract(value, out newCount, Math.Min(count, oldCount));
            return (operation == CountDictionaryOperation.RemovedExistingEntry) || (operation == CountDictionaryOperation.UpdatedExistingEntry);
        }
        
        /// <summary>
        /// Removes a value from the dictionary if it reaches zero or less. The count is expected to never be negative.
        /// </summary>
        /// <param name="value">The value to apply the change to.</param>
        /// <param name="count">The count to subtract. Must not be negative.</param>
        /// <returns>Whether or not the value was removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current state does not permit this method to run.</exception>
        public bool RemoveIfPresent(TValue value, int count = 1) {
            return this.RemoveIfPresent(value, out _, out _, count);
        }

        /// <summary>
        /// Removes a value from the dictionary, if the count reaches less than or equal to zero with a subtraction.
        /// </summary>
        /// <param name="value">The value to apply the change to.</param>
        /// <param name="operation">Output storage for the operation performed.</param>
        /// <param name="newCount">The new count after the subtraction occurs.</param>
        /// <param name="count">The count to subtract. Must not be negative.</param>
        /// <returns>Whether or not the value was removed.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current state does not permit this method to run.</exception>
        public bool RemoveIfPresent(TValue value, out CountDictionaryOperation operation, out int newCount, int count = 1) {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), $"The count to remove cannot be negative. ({count})");
        
            int oldCount = this.GetCount(value);
            if (oldCount < 0)
                throw new InvalidOperationException($"The method {nameof(this.RemoveIfPresent)} assumes the count of value is always >= zero. (Got {oldCount} for {value})");
            if (oldCount == 0 || count == 0) {
                newCount = 0;
                operation = CountDictionaryOperation.None;
                return false;
            }

            operation = this.Subtract(value, out newCount, Math.Min(count, oldCount));
            return (operation == CountDictionaryOperation.RemovedExistingEntry);
        }
    }

    /// <summary>
    /// A description of the operation performed.
    /// </summary>
    public enum CountDictionaryOperation {
        None,
        AddedNewEntry,
        UpdatedExistingEntry,
        RemovedExistingEntry
    }
}