
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace ModToolFramework.Utils.Extensions
{
    /// <summary>
    /// Contains static extensions for enums.
    /// </summary>
    public static class EnumExtensions
    {
        private static readonly Dictionary<Type, EnumNameTracker> CachedEnumNames = new Dictionary<Type, EnumNameTracker>();

        private abstract class EnumNameTracker
        {
            /// <summary>
            /// Gets the name of a particular enum in the cache, or null if it is not set.
            /// </summary>
            /// <param name="enumValue">The enum value to get the cached name of.</param>
            /// <typeparam name="TUnknownEnum">The enum type which this stores.</typeparam>
            /// <returns>cachedName, or null</returns>
            public abstract string GetCachedName<TUnknownEnum>(TUnknownEnum enumValue) where TUnknownEnum : Enum;
            
            /// <summary>
            /// Sets the name of a particular enum in the cache.
            /// </summary>
            /// <param name="enumValue">The value to set the name of.</param>
            /// <param name="name">The name of the value.</param>
            /// <typeparam name="TUnknownEnum">The enum type which this stores.</typeparam>
            public abstract void SetCachedName<TUnknownEnum>(TUnknownEnum enumValue, string name) where TUnknownEnum : Enum;
        }

        private class EnumNameTracker<TEnum> : EnumNameTracker where TEnum : Enum
        {
            private readonly Dictionary<TEnum, string> _cachedNameMap = new Dictionary<TEnum, string>();
            
            /// <inheritdoc cref="EnumNameTracker.GetCachedName{TUnknownEnum}"/>
            public override string GetCachedName<TUnknownEnum>(TUnknownEnum enumValue) {
                if (typeof(TEnum) != typeof(TUnknownEnum))
                    throw new InvalidCastException($"The type {typeof(TUnknownEnum).GetDisplayName()} is not {typeof(TEnum).GetDisplayName()}, the cache's state is invalid..");

                return this._cachedNameMap.TryGetValue(Unsafe.As<TUnknownEnum, TEnum>(ref enumValue), out string enumName) ? enumName : null;
            }
            
            /// <inheritdoc cref="EnumNameTracker.SetCachedName{TUnknownEnum}"/>
            public override void SetCachedName<TUnknownEnum>(TUnknownEnum enumValue, string name) {
                if (typeof(TEnum) != typeof(TUnknownEnum))
                    throw new InvalidCastException($"The type {typeof(TUnknownEnum).GetDisplayName()} is not {typeof(TEnum).GetDisplayName()}, the cache's state is invalid..");
                this._cachedNameMap[Unsafe.As<TUnknownEnum, TEnum>(ref enumValue)] = name;
            }
        }

        private static string GetInvalidName<TEnum>(this TEnum invalidValue) where TEnum : Enum {
            return "INVALID_ENUM_" + invalidValue;
        }

        /// <summary>
        /// Calculates the name of an enum.
        /// This does NOT cache values, meaning this is a very slow method.
        /// For bit flag enums to work properly, they MUST have the C# [Flags] attribute (or [FlagsAttribute]), otherwise they will not be treated as such.
        /// </summary>
        /// <param name="inputEnumValue">The enum value to get the name of.</param>
        /// <param name="invalidReturnsPlaceholder">When an invalid enum is specified and this is true, a name will be generated. Otherwise, null will be returned.</param>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>enumName</returns>
        public static string CalculateName<TEnum>(this TEnum inputEnumValue, bool invalidReturnsPlaceholder = true) where TEnum : struct, Enum {
            Type enumType = typeof(TEnum);
            if (enumType.IsDefined(typeof(FlagsAttribute), false)) { // This enum has bit flags.
                StringBuilder builder = new StringBuilder();
                ulong remainingValue = inputEnumValue.GetAsNumber();

                TEnum[] enumValues = Enum.GetValues<TEnum>();
                Array.Sort(enumValues, (a, b) => a.GetAsNumber().CompareTo(b.GetAsNumber())); // Sort from lowest to highest. Results in a decent order in the output.
                for (int i = enumValues.Length - 1; i >= 0; i--) {
                    TEnum testEnum = enumValues[i];
                    if (!inputEnumValue.HasFlag(testEnum))
                        continue;

                    if (builder.Length > 0)
                        builder.Append(" | ");
                    builder.Append(Enum.GetName(typeof(TEnum), testEnum));

                    remainingValue &= (ulong.MaxValue - testEnum.GetAsNumber()); // Strip out valid flags.
                }
                
                // Add values which are not defined in the enum but still present.
                if (remainingValue > 0) {
                    if (builder.Length > 0)
                        builder.Append(" | ");
                    builder.Append("0x").Append(remainingValue.ToString("X"));
                }
                
                return builder.Length > 0 ? builder.ToString() : "None";
            }
            
            return Enum.GetName(typeof(TEnum), inputEnumValue) ?? (invalidReturnsPlaceholder ? inputEnumValue.GetInvalidName() : null);
        }
        
        /// <summary>
        /// Gets the name of an enum. In the case of an invalid enum, either an invalid name will be created oir
        /// This is magnitudes faster than GetName() because it caches the names. However, this is optional to call 
        /// </summary>
        /// <param name="enumValue">The enum value to get the name of.</param>
        /// <param name="invalidReturnsPlaceholder">When an invalid enum is specified and this is true, a name will be generated. Otherwise, null will be returned.</param>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>enumName</returns>
        public static string GetName<TEnum>(this TEnum enumValue, bool invalidReturnsPlaceholder = true) where TEnum : struct, Enum {
            const string invalidEnum = "@invalid@";
            EnumNameTracker nameTracker = CachedEnumNames.ComputeIfAbsent(typeof(TEnum), () => new EnumNameTracker<TEnum>());

            string name = nameTracker.GetCachedName(enumValue); // Get the cached value.
            bool setName = (name == null);
            name ??= enumValue.CalculateName(false); // If there was no cached value, calculate the name.
            name ??= invalidEnum; // If the name was not calculated, mark the value as invalid.
            
            if (setName)
                nameTracker.SetCachedName(enumValue, name);

            if (invalidEnum.Equals(name, StringComparison.InvariantCulture)) // If the enum value is invalid, determine what to do.
                return invalidReturnsPlaceholder ? enumValue.GetInvalidName() : null;
            return name;
        }
        
        /// <summary>
        /// Gets an enum as the largest unsigned primitive number type.
        /// </summary>
        /// <param name="value">The enum value to get.</param>
        /// <typeparam name="TEnum">The enum's type.</typeparam>
        /// <returns>numericEnumValue</returns>
        public static ulong GetAsNumber<TEnum>(this TEnum value) where TEnum : struct, Enum 
            => (ulong)(object)value;
        
        /// <summary>
        /// Gets an enum as a particular primitive number type.
        /// </summary>
        /// <param name="value">The enum value to get.</param>
        /// <typeparam name="TEnum">The enum's type.</typeparam>
        /// <typeparam name="TNumber">The numeric primitive number type.</typeparam>
        /// <returns>numericEnumValue</returns>
        public static TNumber GetAsNumber<TEnum, TNumber>(this TEnum value) where TEnum : struct, Enum where TNumber : struct 
            => (TNumber)(object)value;

        /// <summary>
        /// Gets the total number of unique values which an enum has.
        /// </summary>
        /// <param name="value">An enum of the type to get value counts from.</param>
        /// <typeparam name="TEnum">The type of enum to get value from</typeparam>
        /// <returns>totalEnumValueCount</returns>
        [SuppressMessage("ReSharper", "UnusedParameter.Global")] // The value is technically used, as the way to get the type.
        public static int GetTotalEnumValueCount<TEnum>(this TEnum value) where TEnum : struct, Enum {
            return EnumUtils.GetTotalEnumValueCount<TEnum>();
        }
    }

    /// <summary>
    /// A class containing static enum utilities.
    /// </summary>
    public static class EnumUtils
    {
        private static readonly Dictionary<Type, int> CachedEnumValueCounts = new Dictionary<Type, int>();

        /// <summary>
        /// Gets the total number of unique values which an enum has.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to get the value count of.</typeparam>
        /// <returns>uniqueEnumValueCount</returns>
        public static int GetTotalEnumValueCount<TEnum>() where TEnum : struct, Enum {
            // This uses GetNames instead of GetValues because if there are duplicate values, it will only count that value once, while GetNames will return the number of unique enum names. (And all must be unique.)
            if (!CachedEnumValueCounts.TryGetValue(typeof(TEnum), out int totalEnumValueCount))
                CachedEnumValueCounts[typeof(TEnum)] = totalEnumValueCount = Enum.GetNames(typeof(TEnum)).Length;
            return totalEnumValueCount;
        }
    }
}