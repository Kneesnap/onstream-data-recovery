
using System;
using System.Collections.Generic;

namespace ModToolFramework.Utils.Extensions
{
    /// <summary>
    /// Contains static extension methods for IEnumerator.
    /// </summary>
    public static class EnumeratorExtensions
    {

        /// <summary>
        /// Goes through all remaining parts of an enumerator until it reaches its end.
        /// </summary>
        /// <param name="enumerator">The enumerator to complete.</param>
        /// <typeparam name="T">The type of value held by the enumerator.</typeparam>
        public static void Close<T>(this IEnumerator<T> enumerator) {
            while (enumerator.MoveNext()) {
            }
        }
        
        /// <summary>
        /// Converts an IEnumerator into a List.
        /// </summary>
        /// <param name="enumerator">The enumerator to convert.</param>
        /// <typeparam name="T">The element type which the IEnumerator holds.</typeparam>
        /// <returns>convertedList</returns>
        public static List<T> ToList<T>(this IEnumerator<T> enumerator) {
            List<T> outputList = new List<T>();
            while (enumerator.MoveNext())
                outputList.Add(enumerator.Current);
            return outputList;
        }

        /// <summary>
        /// Converts an IEnumerator into a List.
        /// </summary>
        /// <param name="enumerator">The enumerator to convert.</param>
        /// <param name="existingList">The existing list to write values to.</param>
        /// <typeparam name="T">The element type which the IEnumerator holds.</typeparam>
        /// <returns>convertedList</returns>
        public static List<T> ToList<T>(this IEnumerator<T> enumerator, List<T> existingList) {
            if (existingList == null)
                throw new ArgumentNullException(nameof(existingList));
            
            existingList.Clear();
            while (enumerator.MoveNext())
                existingList.Add(enumerator.Current);
            return existingList;
        }
        
        /// <summary>
        /// Converts an IEnumerator into a HashSet.
        /// </summary>
        /// <param name="enumerator">The enumerator to convert.</param>
        /// <typeparam name="T">The element type which the IEnumerator holds.</typeparam>
        /// <returns>convertedSet</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerator<T> enumerator) {
            HashSet<T> outputSet = new HashSet<T>();
            while (enumerator.MoveNext())
                outputSet.Add(enumerator.Current);
            return outputSet;
        }

        /// <summary>
        /// Converts an IEnumerator into a HashSet.
        /// </summary>
        /// <param name="enumerator">The enumerator to convert.</param>
        /// <param name="existingSet">The existing set to write values to.</param>
        /// <typeparam name="T">The element type which the IEnumerator holds.</typeparam>
        /// <returns>convertedSet</returns>
        public static HashSet<T> ToHashSet<T>(this IEnumerator<T> enumerator, HashSet<T> existingSet) {
            if (existingSet == null)
                throw new ArgumentNullException(nameof(existingSet));
            
            existingSet.Clear();
            while (enumerator.MoveNext())
                existingSet.Add(enumerator.Current);
            return existingSet;
        }

        /// <summary>
        /// Converts an IEnumerator into an array.
        /// </summary>
        /// <param name="enumerator">The enumerator to convert.</param>
        /// <typeparam name="T">The element type which the IEnumerator holds.</typeparam>
        /// <returns>convertedArray</returns>
        public static T[] ToArray<T>(this IEnumerator<T> enumerator) {
            return ToList(enumerator).ToArray();
        }
        
    }
}