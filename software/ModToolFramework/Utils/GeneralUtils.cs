using ModToolFramework.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ModToolFramework.Utils {
    /// <summary>
    /// Contains general static utilities used by anything.
    /// </summary>
    public static class GeneralUtils {
        private static readonly Random Random = new Random();
        private static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Gets the current time in milliseconds since (1/1/1970 00:00:00). (Standard unix time.)
        /// This should not be used for performance measuring, as it may not be fast enough.
        /// </summary>
        public static long CurrentTimeMillis => DateTimeOffset.Now.ToUnixTimeMilliseconds();

        /// <summary>
        /// Converts a millisecond timestamp value to DateTime.
        /// </summary>
        /// <param name="timeMs">The timestamp to convert.</param>
        /// <returns>Returns a DateTime object</returns>
        public static DateTime ConvertTimestampToDateTime(long timeMs) {
            return EpochStart.AddMilliseconds(timeMs).ToLocalTime();
        }

        /// <summary>
        /// Converts a DateTime object into a millisecond timestamp.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>timestampMs</returns>
        public static long ConvertDateTimeToUnixTime(DateTime dateTime) {
            return (long)(dateTime - EpochStart).TotalMilliseconds;
        }

        /// <summary>
        /// Gets the amount of time between the supplied time and now.
        /// Works both with times in the future and times in the past.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns>timeDisplayStr</returns>
        public static string GetTimeDifferenceDisplay(DateTime time) {
            DateTime now = DateTime.Now;
            bool timeInPast = now >= time;
            TimeSpan difference = timeInPast ? now - time : time - now;
            if (difference.Days > 1) {
                return (timeInPast ? time : now).ToString(CultureInfo.CurrentCulture); // That's a ternary to avoid unnecessary duplication.
            }

            if (difference.Days == 1) {
                return "Yesterday @ " + (timeInPast ? time : now).ToString("hh:mm tt", CultureInfo.CurrentCulture);
            }

            if (difference.Hours >= 1) {
                return difference.Hours + " Hours";
            }

            return difference.Minutes + " Minutes";
        }

        /// <summary>
        /// Replaces the element at the particular index, or adds a new element to the list.
        /// </summary>
        /// <param name="list">The list to perform operations on.</param>
        /// <param name="index">The index to apply the value to.</param>
        /// <param name="element">The element to apply.</param>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid index is specified.</exception>
        public static void AddOrReplace<TElement>(this List<TElement> list, int index, TElement element) {
            if (index < 0 || index > list.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Index out of range {list.GetRangeString()}.");
            if (index == list.Count) {
                list.Add(element);
            } else {
                list[index] = element;
            }
        }
        
        /// <summary>
        /// Gets the last element in a list.
        /// </summary>
        /// <param name="list">The list to get the last element from.</param>
        /// <typeparam name="TElement">The element type.</typeparam>
        public static TElement GetLast<TElement>(this List<TElement> list) {
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot get last value from empty list!");
            return list[^1];
        }
        
        /// <summary>
        /// Gets the last element in a list, or default if the list is empty.
        /// </summary>
        /// <param name="list">The list to get the last element from.</param>
        /// <typeparam name="TElement">The element type.</typeparam>
        public static TElement GetLastOrDefault<TElement>(this List<TElement> list) {
            return list.Count > 0 ? list[^1] : default;
        }
        
        /// <summary>
        /// Removes the first element in a list.
        /// </summary>
        /// <param name="list">The list to perform operations on.</param>
        /// <typeparam name="TElement">The element type.</typeparam>
        public static TElement RemoveFirst<TElement>(this List<TElement> list) {
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot remove value from empty list!");

            TElement value = list[0];
            list.RemoveAt(0);
            return value;
        }
        
        /// <summary>
        /// Removes the last element in a list.
        /// </summary>
        /// <param name="list">The list to perform operations on.</param>
        /// <typeparam name="TElement">The element type.</typeparam>
        public static TElement RemoveLast<TElement>(this List<TElement> list) {
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot remove value from empty list!");

            int index = list.Count - 1;
            TElement value = list[index];
            list.RemoveAt(index);
            return value;
        }

        /// <summary>
        /// Clones a given array.
        /// </summary>
        /// <typeparam name="T">The array value type.</typeparam>
        /// <param name="array">The array to clone</param>
        /// <param name="startIndex">The first index to copy from the original array.</param>
        /// <param name="length">How many elements to copy from the original array. If length is -1, it will all remaining elements in the array.</param>
        /// <returns>subArray</returns>
        public static T[] CloneArray<T>(this T[] array, int startIndex = 0, int length = -1) {
            if (length <= -1)
                length = Math.Max(0, array.Length - startIndex);

            T[] subset = new T[length];
            Array.Copy(array, startIndex, subset, 0, length);
            return subset;
        }
        
        /// <summary>
        /// Tests if all of the elements in the array are the default.
        /// </summary>
        /// <typeparam name="T">The array value type.</typeparam>
        /// <param name="array">The array to test</param>
        /// <returns>subArray</returns>
        public static bool IsDefaultArray<T>(this T[] array) {
            T defaultElement = default(T);
            for (int i = 0; i < array.Length; i++)
                if (!Equals(array[i], defaultElement))
                    return false;

            return true;
        }

        /// <summary>
        /// Gets a string representation of the array values.
        /// </summary>
        /// <typeparam name="T">The array value type.</typeparam>
        /// <param name="array">The array to clone</param>
        /// <param name="startIndex">The first index to show from the array.</param>
        /// <param name="length">How many elements to show from the original array. If length is -1, it read until the end of the array.</param>
        /// <param name="toStringFunction">A custom function to get the string value of an element.</param>
        /// <returns>subArray</returns>
        public static string ToDisplayString<T>(this T[] array, int startIndex = 0, int length = -1, Func<T, string> toStringFunction = null) {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Start index cannot be less than zero. ({startIndex}/{array.Length})");
            if (startIndex >= array.Length && array.Length > 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), $"Start index cannot be beyond the array length! ({startIndex}/{array.Length})");
            if (length <= -1)
                length = Math.Max(0, array.Length - startIndex);
            if (length == 0)
                return "[]";
            if (startIndex + length > array.Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"Too many elements to get from array! (Start Index: {startIndex}, Length: {length}, Array Length: {array.Length})");
            if (array is byte[] byteArray)
                return "[" + DataUtils.ToString(byteArray) + "]";
            
            StringBuilder stringBuilder = new StringBuilder("[");
            for (int i = startIndex; i < startIndex + length; i++) {
                if (i > startIndex)
                    stringBuilder.Append(", ");

                T element = array[i];
                if (element == null) {
                    stringBuilder.Append("null");
                } else {
                    stringBuilder.Append(toStringFunction?.Invoke(element) ?? element.ToString());
                }
            }

            return stringBuilder.Append(']').ToString();
        }

        /// <summary>
        /// Returns an array of the given size with default values.
        /// It will keep the existing array if it is the correct size, which is helpful for code where allocations need to be monitored.
        /// </summary>
        /// <param name="array">The existing array.</param>
        /// <param name="newSize">The new array size.</param>
        /// <typeparam name="T">The element type of values in the array.</typeparam>
        /// <returns>array</returns>
        public static T[] ResizeClear<T>(T[] array, int newSize) {
            if (array == null || array.Length != newSize)
                return new T[newSize];
            for (int i = 0; i < array.Length; i++)
                array[i] = default;
            return array;
        }

        /// <summary>
        /// Creates a copy of the array with an arbitrary number of elements removed from the beginning.
        /// </summary>
        /// <param name="array">The existing array.</param>
        /// <param name="elementsToRemove">The number of elements to remove.</param>
        /// <typeparam name="T">The element type of values in the array.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if you want to remove more elements than the array holds.</exception>
        /// <returns>array</returns>
        public static T[] RemoveArrayElements<T>(T[] array, int elementsToRemove) {
            if (elementsToRemove == 0)
                return array;

            if (array == null || elementsToRemove > array.Length || elementsToRemove < 0)
                throw new ArgumentOutOfRangeException($"Could not remove elements from the array! (Length: {array?.Length}, Remove: {elementsToRemove})");

            T[] smallerArray = new T[array.Length - elementsToRemove];
            Array.Copy(array, elementsToRemove, smallerArray, 0, smallerArray.Length);
            return smallerArray;
        }

        /// <summary>
        /// Reverses the order of all elements in an array.
        /// </summary>
        /// <typeparam name="T">The array value type.</typeparam>
        /// <param name="array">The array to reverse.</param>
        /// <returns>reversedInputArray</returns>
        public static T[] ReverseArray<T>(T[] array) {
            return ReverseArray(array, array.Length);
        }

        /// <summary>
        /// Reverses an array up until a certain index.
        /// </summary>
        /// <typeparam name="T">The type of values in the array.</typeparam>
        /// <param name="array">The array to reverse.</param>
        /// <param name="maxIndex">The index to stop reversing values at.</param>
        /// <returns>reversedInputArray</returns>
        public static T[] ReverseArray<T>(T[] array, int maxIndex) {
            for (int i = 0; i < maxIndex / 2; i++) {
                T temp = array[i];
                array[i] = array[maxIndex - i - 1];
                array[maxIndex - i - 1] = temp;
            }

            return array;
        }

        /// <summary>
        /// Gets the name of the method which called the method which called this function.
        /// </summary>
        /// <returns>callingMethodName</returns>
        public static string GetCallingMethodName() {
            StackFrame frame = new StackTrace().GetFrame(2);
            MethodBase methodBase = frame?.GetMethod();
            return methodBase != null ? (methodBase.DeclaringType != null ? methodBase.DeclaringType.Name + "." : string.Empty) + methodBase.Name : null;
        }

        /// <summary>
        /// Prints the current stack trace. (Expensive call, for debugging only.)
        /// </summary>
        public static void PrintStackTrace() {
            Console.WriteLine(Environment.StackTrace);
        }

        /// <summary>
        /// Prints an exception to the console.
        /// </summary>
        /// <param name="exception">The exception to print.</param>
        public static void PrintStackTrace(Exception exception) {
            Console.WriteLine(ToLogString(exception));
        }

        /// <summary>
        /// Provides full stack trace for the exception that occurred.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        public static string ToLogString(Exception exception) {
            return ToLogString(exception, Environment.StackTrace);
        }

        /// <summary>
        /// Provides full stack trace for the exception that occurred.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        /// <param name="environmentStackTrace">Environment stack trace, for pulling additional stack frames.</param>
        public static string ToLogString(Exception exception, string environmentStackTrace) {
            List<string> environmentStackTraceLines = GetUserStackTraceLines(environmentStackTrace);
            environmentStackTraceLines.RemoveAt(0);

            List<string> stackTraceLines = GetStackTraceLines(exception.StackTrace);
            stackTraceLines.AddRange(environmentStackTraceLines);

            string fullStackTrace = String.Join(Environment.NewLine, stackTraceLines);

            return (exception.InnerException != null ? ToLogString(exception.InnerException, environmentStackTrace) + Environment.NewLine : string.Empty)
                   + exception.Message + Environment.NewLine + fullStackTrace;
        }

        /// <summary>
        /// Gets a list of stack frame lines, as strings.
        /// </summary>
        /// <param name="stackTrace">Stack trace string.</param>
        private static List<string> GetStackTraceLines(string stackTrace) {
            return stackTrace.Split(new[] {Environment.NewLine}, StringSplitOptions.None).ToList();
        }

        /// <summary>
        /// Gets a list of stack frame lines, as strings, only including those for which line number is known.
        /// </summary>
        /// <param name="fullStackTrace">Full stack trace, including external code.</param>
        private static List<string> GetUserStackTraceLines(string fullStackTrace) {
            Regex regex = new Regex(@"([^\)]*\)) in (.*):line (\d)*$");
            return GetStackTraceLines(fullStackTrace)
                .Where(stackTraceLine => regex.IsMatch(stackTraceLine)).ToList();
        }

        /// <summary>
        /// Turns a file into a read-only memory stream.
        /// It's faster to buffer the entire file in memory, and just do one full read and one full write.
        /// However, this might not be optimal in situations where there are huge data files.
        /// </summary>
        /// <param name="filePath">The path to the file to load.</param>
        /// <returns>fileMemoryStream</returns>
        public static MemoryStream ReadMemoryStreamFromFile(string filePath) {
            return new MemoryStream(File.ReadAllBytes(filePath));
        }

        /// <summary>
        /// Read all bytes from a stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>readData</returns>
        public static byte[] ReadBytesFromStream(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            byte[] resultData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(resultData, 0, resultData.Length);
            return resultData;
        }

        /// <summary>
        /// Saves the bytes in a stream to a file, deleting the file if it already exists.
        /// </summary>
        /// <param name="stream">The stream to save.</param>
        /// <param name="path">The file path.</param>
        /// <exception cref="ArgumentNullException">Thrown if null is provided.</exception>
        public static void SaveStreamToFile(Stream stream, string path) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (File.Exists(path))
                File.Delete(path);
            File.WriteAllBytes(path, ReadBytesFromStream(stream));
        }

        /// <summary>
        /// Strips the last extension from a file path. Leaves any previous extensions, or leaves the string as-is if there is no extension.
        /// </summary>
        /// <param name="filePath">The file path to strip the extension from</param>
        /// <returns>strippedFilePath</returns>
        [SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
        public static string StripExtension(string filePath) {
            int lastIndex = filePath.LastIndexOf(".", StringComparison.InvariantCulture);
            int lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            return lastIndex != -1 && lastIndex > lastSeparator ? filePath.Substring(0, lastIndex) : filePath;
        }

        /// <summary>
        /// Test if a char is a hexadecimal digit.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns>IsHexadecimal</returns>
        public static bool IsHexadecimal(char val) {
            return IsDigit(val) || (val >= 'A' && val <= 'F') || (val >= 'a' && val <= 'f');
        }

        /// <summary>
        /// Test if a char is a numeric digit.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns>isDigit</returns>
        public static bool IsDigit(char val) {
            return val >= '0' && val <= '9';
        }

        /// <summary>
        /// Test if a char is an English letter.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns>IsLetter</returns>
        public static bool IsLetter(char val) {
            return (val >= 'a' && val <= 'z') || (val >= 'A' && val <= 'Z');
        }

        /// <summary>
        /// Test if a char is whitespace.
        /// </summary>
        /// <param name="val">The char to test.</param>
        /// <returns>isWhitespace</returns>
        public static bool IsWhitespace(char val) {
            return val == '\t' || val == '\r' || val == ' ';
        }

        /// <summary>
        /// Test if a char is a letter or a digit.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns>IsLetterOrDigit</returns>
        public static bool IsLetterOrDigit(char val) {
            return IsDigit(val) || IsLetter(val);
        }

        /// <summary>
        /// Test if a char is a letter or a digit.
        /// </summary>
        /// <param name="val">The value to test.</param>
        /// <returns>IsLetterOrDigit</returns>
        public static bool IsWordCharacter(char val) {
            return IsDigit(val) || IsLetter(val) || val == '_';
        }

        /// <summary>
        /// Tests if a string is alpha-numeric.
        /// </summary>
        /// <param name="testString">The string to test.</param>
        /// <returns>IsAlphanumeric</returns>
        public static bool IsAlphanumeric(string testString) {
            return testString != null && testString.All(IsLetterOrDigit);
        }

        /// <summary>
        /// Gets the number of digits a certain number will take up.
        /// </summary>
        /// <param name="num">The number to get the digit count of.</param>
        /// <returns>The number of digits used.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if a negative number is supplied.</exception>
        public static int GetDigitCount(int num) {
            if (num < 0)
                throw new ArgumentOutOfRangeException($"Cannot get digit count for negative number: {num}.");
            if (num == 0)
                return 1;
            return (int)Math.Log10(num) + 1;
        }

        /// <summary>
        /// Creates a numeric string, with '0' padding until it reaches a certain length.
        /// eg. 4 -> "004", or with the same settings, 123 -> "123".
        /// </summary>
        /// <param name="number">The number to use.</param>
        /// <param name="digits">The required number of digits.</param>
        /// <returns>paddedStr</returns>
        public static string PadNumberString(int number, int digits) {
            int usedDigits = GetDigitCount(number);

            StringBuilder prependStr = new StringBuilder();
            for (int i = 0; i < digits - usedDigits; i++)
                prependStr.Append('0');

            return prependStr.ToString() + number;
        }

        /// <summary>
        /// Adds left-padding to a string.
        /// </summary>
        /// <param name="baseStr">The string to pad.</param>
        /// <param name="targetLength">The target length which should be padded to.</param>
        /// <param name="toAdd">The padding character to use.</param>
        /// <returns>paddedStr</returns>
        public static string PadStringLeft(string baseStr, int targetLength, char toAdd) {
            StringBuilder prependStr = new StringBuilder();
            while (targetLength > prependStr.Length + baseStr.Length)
                prependStr.Append(toAdd);

            return prependStr + baseStr;
        }
        
        /// <summary>
        /// Adds right-padding to a string.
        /// </summary>
        /// <param name="baseStr">The string to pad.</param>
        /// <param name="targetLength">The target length which should be padded to.</param>
        /// <param name="toAdd">The padding character to use.</param>
        /// <returns>paddedStr</returns>
        public static string PadStringRight(string baseStr, int targetLength, char toAdd) {
            StringBuilder appendStr = new StringBuilder();
            while (targetLength > appendStr.Length + baseStr.Length)
                appendStr.Append(toAdd);

            return baseStr + appendStr;
        }

        /// <summary>
        /// Capitalize the first character of the string (if it's alphabetic), and lower-case the rest. Every word (indicated by a space) will also be capitalized.
        /// </summary>
        /// <param name="sentence">The string to capitalize.</param>
        /// <returns>capitalizedStr</returns>
        public static string Capitalize(string sentence) {
            string[] split = sentence.Split(" ");

            for (int i = 0; i < split.Length; i++) {
                if (split[i].Length > 0) {
                    split[i] = Char.ToUpper(split[i][0]) +
                               (split[i].Length > 1 ? split[i].Substring(1).ToLower() : string.Empty);
                }
            }

            return String.Join(" ", split);
        }

        /// <summary>
        /// Test if a class is an instance of a parent class while ignoring the generic.
        /// </summary>
        /// <param name="genericParentType">The parent class with a generic.</param>
        /// <param name="testSubclassType">The subclass type to test.</param>
        /// <returns>IsLegitSubclass</returns>
        public static bool IsValidSubclassIgnoreGeneric(Type genericParentType, Type testSubclassType) {
            Type checkType = testSubclassType;
            while (checkType != null) {
                if (genericParentType.IsAssignableFrom(checkType.IsGenericType ? checkType.GetGenericTypeDefinition() : checkType))
                    return true;
                checkType = checkType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Test if a class is an instance of a parent class while making sure the generic(s) are acceptable too.
        /// </summary>
        /// <param name="genericParentType">The parent class with a generic.</param>
        /// <param name="testSubclassType">The subclass to test.</param>
        /// <returns>IsLegitSubclassWithGeneric</returns>
        public static bool IsValidSubclassRequireGeneric(Type genericParentType, Type testSubclassType) {
            if (!genericParentType.IsGenericType)
                throw new InvalidDataException(nameof(genericParentType) + " is not a generic type!");

            Type checkType = testSubclassType;
            while (checkType != null) {
                if (checkType.IsGenericType && genericParentType == checkType) {
                    return true;
                }

                checkType = checkType.BaseType;
            }

            return false;
        }

        /// <summary>
        /// Shuffles an array in-place.
        /// </summary>
        /// <param name="array">The array to shuffle.</param>
        /// <param name="random">The random number generator to use. If null is supplied, the default Random will be used.</param>
        /// <typeparam name="T">The array value type.</typeparam>
        public static void Shuffle<T>(T[] array, Random random = null) {
            random ??= Random;

            int n = array.Length;
            while (n > 1) {
                int k = random.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
        
        /// <summary>
        /// Generates a number of true bits into an unsigned long.
        /// </summary>
        /// <param name="bitCount">The number of bits to generate.</param>
        /// <returns>generatedBits</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid number of bits is specified.</exception>
        public static ulong GenerateBits(int bitCount) {
            const int maxBitCount = DataConstants.BitsPerByte * DataConstants.LongSize;
            if (bitCount > maxBitCount)
                throw new ArgumentOutOfRangeException($"There is not enough space in a 64-bit long for {bitCount} bits.");
            if (bitCount < 0)
                throw new ArgumentOutOfRangeException($"Cannot generate a negative number of bits! ({bitCount})");
            return bitCount == maxBitCount ? 0xFFFFFFFFFFFFFFFFUL : ((1U << bitCount) - 1U);
        }

        /// <summary>
        /// Swaps two values.
        /// </summary>
        /// <param name="left">The first element to swap.</param>
        /// <param name="right">The right element to swap.</param>
        /// <typeparam name="TElement">The type of element to swap.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<TElement>(ref TElement left, ref TElement right) {
            TElement temp = left;
            left = right;
            right = temp;
        }

        /// <summary>
        /// Returns the second value (b)'s relation to the first value (a).
        /// </summary>
        /// <param name="a">The first value, this is what b is determined to be in relation to.</param>
        /// <param name="b">The second value.</param>
        /// <typeparam name="TValue">The value which gets compared.</typeparam>
        /// <returns>comparableResult</returns>
        public static ComparableResult GetComparableResult<TValue>(this TValue a, TValue b) where TValue : IComparable<TValue> {
            return GetComparableResult(b.CompareTo(a));
        }

        /// <summary>
        /// Converts an integer into a ComparableResult.
        /// </summary>
        /// <param name="input">The number to convert into a ComparableResult.</param>
        /// <returns>comparableResult</returns>
        public static ComparableResult GetComparableResult(int input) {
            return input switch {
                > 0 => ComparableResult.Future,
                < 0 => ComparableResult.Previous,
                _ => ComparableResult.Current
            };
        }

        public static ComparableResult Invert(this ComparableResult inputResult) {
            return inputResult switch {
                ComparableResult.Previous => ComparableResult.Future,
                ComparableResult.Future => ComparableResult.Previous,
                ComparableResult.Current => ComparableResult.Current,
                _ => throw new InvalidEnumArgumentException($"Unsupported {nameof(ComparableResult)} enum value '{inputResult.CalculateName()}' provided.")
            };
        }
    }
    
    /// <summary>
    /// This enum represents the different return options which IComparable can output.
    /// The values in this enum can be returned in compare methods and they will work.
    /// </summary>
    public enum ComparableResult
    {
        Previous = -1, // a > b
        Current = 0, // a == b
        Future = 1 // a < b.
    }
}