using System;
using System.Collections.Generic;

namespace ModToolFramework.Utils.Extensions
{
    /// <summary>
    /// Static HashSet extensions.
    /// </summary>
    public static class HashSetExtensions
    {
        /// <summary>
        /// Adds all the remaining elements in an enumerator to the hash set.
        /// </summary>
        /// <param name="set">The hash set to add values to.</param>
        /// <param name="enumerable">The enumerable to add values from.</param>
        /// <typeparam name="TValue">The type of value the hash set stores.</typeparam>
        public static void AddAll<TValue>(this HashSet<TValue> set, IEnumerable<TValue> enumerable) {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                set.Add(enumerator.Current);
        }
        
        /// <summary>
        /// Adds all the remaining elements in an enumerator to the hash set.
        /// </summary>
        /// <param name="set">The hash set to add values to.</param>
        /// <param name="enumerator">The enumerator to add values from.</param>
        /// <typeparam name="TValue">The type of value the hash set stores.</typeparam>
        public static void AddAll<TValue>(this HashSet<TValue> set, IEnumerator<TValue> enumerator) {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator));
            
            while (enumerator.MoveNext())
                set.Add(enumerator.Current);
        }
    }
}