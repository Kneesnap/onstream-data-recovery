using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ModToolFramework.Utils
{
    /// <summary>
    /// A wrapper around a struct which performs value-type equality checks, meaning it works in HashSet / Dictionary / List / etc.
    /// </summary>
    public class StructWrapper<TStruct>
        where TStruct : struct
    {
        /// <summary>
        /// The wrapped struct value.
        /// </summary>
        public TStruct Value;

        /// <summary>
        /// Creates a new instance of <see cref="StructWrapper{TStruct}"/> with a pre-existing value.
        /// </summary>
        /// <param name="value">The value to copy to the struct.</param>
        public StructWrapper(ref TStruct value) {
            this.Value = value;
        }
        
        /// <summary>
        /// Creates a new instance of <see cref="StructWrapper{TStruct}"/> with a pre-existing value.
        /// </summary>
        /// <param name="value">The value to copy to the struct.</param>
        public StructWrapper(TStruct value) {
            this.Value = value;
        }

        /// <summary>
        /// Creates a new instance of <see cref="StructWrapper{TStruct}"/> using the default value type.
        /// </summary>
        public StructWrapper() : this(default) {
        }

        /// <inheritdoc cref="object.Equals(object)"/>
        public override bool Equals(object obj) {
            return (obj is StructWrapper<TStruct> otherWrapper) && otherWrapper.Value.Equals(this.Value);
        }

        /// <inheritdoc cref="object.GetHashCode"/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() {
            return this.Value.GetHashCode();
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return "StructWrapper<" + typeof(TStruct).Name + ">{" + this.Value + "}";
        }
    }

    public static class StructWrapperConversionExtensions
    {
        /// <summary>
        /// Gets a list of TValue without their wrappers from a list of <see cref="StructWrapper{TStruct}"/>
        /// </summary>
        /// <param name="wrapperList">The list containing the wrappers remove.</param>
        /// <typeparam name="TValue">The type of value which the StructWrapper holds.</typeparam>
        /// <returns>unwrappedList</returns>
        public static List<TValue> RemoveWrapper<TValue>(this List<StructWrapper<TValue>> wrapperList) 
            where TValue : struct
            => wrapperList.ConvertAll(wrapper => wrapper.Value).ToList();
        
        /// <summary>
        /// Gets a list of TValue without their wrappers from an immutable list of <see cref="StructWrapper{TStruct}"/>
        /// </summary>
        /// <param name="wrapperList">The list containing the wrappers remove.</param>
        /// <typeparam name="TValue">The type of value which the StructWrapper holds.</typeparam>
        /// <returns>unwrappedList</returns>
        public static List<TValue> RemoveWrapper<TValue>(this ImmutableList<StructWrapper<TValue>> wrapperList) 
            where TValue : struct
            => wrapperList.ConvertAll(wrapper => wrapper.Value).ToList();
    }
}