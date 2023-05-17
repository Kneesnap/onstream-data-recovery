namespace ModToolFramework.Utils
{
    /// <summary>
    /// A reference type wrapper around a struct.
    /// </summary>
    public class StructRef<TStruct>
        where TStruct : struct
    {
        /// <summary>
        /// The wrapped struct value.
        /// </summary>
        public TStruct Value;

        /// <summary>
        /// Creates a new instance of <see cref="StructRef{TStruct}"/> with a pre-existing value.
        /// </summary>
        /// <param name="value">The value to copy to the struct.</param>
        public StructRef(ref TStruct value) {
            this.Value = value;
        }
        
        /// <summary>
        /// Creates a new instance of <see cref="StructRef{TStruct}"/> with a pre-existing value.
        /// </summary>
        /// <param name="value">The value to copy to the struct.</param>
        public StructRef(TStruct value) {
            this.Value = value;
        }

        /// <summary>
        /// Creates a new instance of <see cref="StructRef{TStruct}"/> using the default value type.
        /// </summary>
        public StructRef() : this(default) {
        }

        /// <summary>
        /// Tests if the values of two <see cref="StructRef{TStruct}"/> match.
        /// </summary>
        /// <param name="other">The other reference to check.</param>
        /// <returns>valuesMatch</returns>
        public bool ValueEquals(StructRef<TStruct> other) {
            return (other != null) && this.Value.Equals(other.Value);
        }
    }
}