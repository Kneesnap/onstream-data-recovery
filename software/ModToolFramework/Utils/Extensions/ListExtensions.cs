using System;
using System.Collections.Generic;

namespace ModToolFramework.Utils.Extensions
{
    /// <summary>
    /// A class containing static list extensions.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Gets a string describing the range of the list. Mainly used for error messages.
        /// </summary>
        /// <param name="list">The list to get the range of indices for.</param>
        /// <typeparam name="TElement">The list's element type.</typeparam>
        /// <returns>The range string.</returns>
        public static string GetRangeString<TElement>(this List<TElement> list) {
            return (list != null && list.Count > 0) ? "[0, " + list.Count + ")" : "[None]";
        }

        /// <summary>
        /// Adds all the remaining elements in an enumerator to the list.
        /// </summary>
        /// <param name="list">The list to add values to.</param>
        /// <param name="enumerable">The enumerable to add values from.</param>
        /// <typeparam name="TValue">The type of value the list stores.</typeparam>
        public static void AddAll<TValue>(this List<TValue> list, IEnumerable<TValue> enumerable) {
            if (enumerable == null)
                throw new ArgumentNullException(nameof(enumerable));

            using IEnumerator<TValue> enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);
        }
        
        /// <summary>
        /// Adds all the remaining elements in an enumerator to the list.
        /// </summary>
        /// <param name="list">The list to add values to.</param>
        /// <param name="enumerator">The enumerator to add values from.</param>
        /// <typeparam name="TValue">The type of value the list stores.</typeparam>
        public static void AddAll<TValue>(this List<TValue> list, IEnumerator<TValue> enumerator) {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator));
            
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);
        }
    }
}