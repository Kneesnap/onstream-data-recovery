using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ModToolFramework.Utils {
    /// <summary>
    /// Contains various static extensions.
    /// </summary>
    public static class StaticExtensions {
        /// <summary>
        /// Returns the value for a corresponding key in a dictionary, or creates the value if it does not exist. (Returns the new value in that case.)
        /// </summary>
        /// <param name="dictionary">The dictionary to get the value from.</param>
        /// <param name="key">The key to get the value from the dictionary with.</param>
        /// <param name="maker">The maker used to create the value if it does not exist.</param>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <returns>valueInDictionary</returns>
        public static TValue ComputeIfAbsent<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> maker) {
            if (!dictionary.TryGetValue(key, out TValue value))
                dictionary.Add(key, value = maker.Invoke());
            return value;
        }

        /// <summary>
        /// Returns the value for a corresponding key in a dictionary, or creates the value if it does not exist. (Returns the new value in that case.)
        /// </summary>
        /// <param name="dictionary">The dictionary to get the value from.</param>
        /// <param name="key">The key to get the value from the dictionary with.</param>
        /// <param name="maker">The maker used to create the value if it does not exist.</param>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <returns>valueInDictionary</returns>
        public static TValue ComputeIfAbsent<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> maker) {
            if (!dictionary.TryGetValue(key, out TValue value))
                dictionary.Add(key, value = maker.Invoke(key));
            return value;
        }

        // Type Extensions:

        /// <summary>
        /// Gets the display name of a Type, including generic parameters.
        /// This is not cached, because the assumption is that this is mainly used for debug logging.
        /// </summary>
        /// <param name="type">The type to get the name of.</param>
        /// <param name="recursionLayer">Used for recursive calls. Don't touch this.</param>
        /// <returns>displayName</returns>
        [SuppressMessage("ReSharper", "ReplaceSubstringWithRangeIndexer")]
        public static string GetDisplayName(this Type type, int recursionLayer = 0) {
            if (type == null)
                return "null";
            if (!type.IsGenericType) {
                if (type.IsPrimitive) {
                    return type.Name.Substring(type.Name.LastIndexOf('.') + 1).ToLowerInvariant();
                } else if (type.IsArray) {
                    return type.GetElementType().GetDisplayName(recursionLayer + 1) + "[]";
                } else {
                    return type.Name; 
                }
            } else if (recursionLayer >= 10) {
                return "..."; // Prevents infinite loops.
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(type.Name.Split("`")[0]).Append('<');
            
            // Build generic string.
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; i++) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(typeParameters[i].GetDisplayName(recursionLayer + 1));
            }

            return sb.Append('>').ToString();
        }

        /// <summary>
        /// Gets the display name of an object's type, including generic parameters.
        /// </summary>
        /// <param name="objectInstance">The object whose type we should get the name of.</param>
        /// <returns>displayName</returns>
        public static string GetTypeDisplayName<T>(this T objectInstance) {
            return objectInstance?.GetType().GetDisplayName() ?? "Null{No Type}";
        }
    }
}