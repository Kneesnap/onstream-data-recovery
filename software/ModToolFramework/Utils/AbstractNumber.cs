using ModToolFramework.Utils.Data;
using ModToolFramework.Utils.Extensions;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace ModToolFramework.Utils {
    /// <summary>
    /// This class represents a number of an arbitrary type.
    /// This can be quite useful when dealing with situations where the types of each number can vary.
    /// An obvious example of this is writing a language (or even a language preprocessor), where you may need to add two numbers of differing types together.
    ///
    /// Type Modes:
    /// There are two type modes which operations can be performed with.
    /// #1 Target Typing: The operation will be performed with a specific type target. ((byte)10 * (int)260) = (byte)
    /// #2 Automatic Typing: A new type will automatically be selected if necessary, with respect to sign. ((byte)10 * (int)260) = (ushort)2600
    ///
    /// Types can change from:
    ///  - Float vs Integer (If either is a floating point type, it will use the type which is FP. If both are floating point types, it will use the largest type of either.)
    ///  - Differing Size (The smallest type which can hold the number accurately will be used, while retaining whether or not the number is signed.)
    /// Useful Documentation:
    ///  - https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/numeric-conversions
    ///  - https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types
    ///  - https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types
    ///  - https://cs.stackexchange.com/questions/105398/signed-and-unsigned-numbers (Shows that some operations are not the same for signed vs unsigned.)
    ///
    /// The goal is to allow performing basic math operations on numbers of dynamic types.
    /// </summary>
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
    public class AbstractNumber {
        private sbyte _sbyteValue;
        private byte _byteValue;
        private short _shortValue;
        private ushort _ushortValue;
        private int _intValue;
        private uint _uintValue;
        private long _longValue;
        private ulong _ulongValue;
        private Half _halfValue;
        private float _floatValue;
        private double _doubleValue;

        /// <summary>
        /// Gets the number type which this number is.
        /// </summary>
        public AbstractNumberType Type { get; private set; }

        /// <summary>
        /// Represents the numeric value as an sbyte.
        /// </summary>
        public sbyte SByteValue {
            get => this._sbyteValue;
            set {
                this._sbyteValue = value;
                this._byteValue = (byte)(value & 0xFF);
                this._shortValue = value;
                this._ushortValue = (ushort)(value & 0xFF);
                this._intValue = value;
                this._uintValue = (uint)(value & 0xFF);
                this._longValue = value;
                this._ulongValue = (ulong)(value & 0xFF);
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.SByte;
            }
        }

        /// <summary>
        /// Represents the numeric value as a byte.
        /// </summary>
        public byte ByteValue {
            get => this._byteValue;
            set {
                this._sbyteValue = unchecked((sbyte)value);
                this._byteValue = value;
                this._shortValue = value;
                this._ushortValue = value;
                this._intValue = value;
                this._uintValue = value;
                this._longValue = value;
                this._ulongValue = value;
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.Byte;
            }
        }

        /// <summary>
        /// Represents the numeric value as a short.
        /// </summary>
        public short ShortValue {
            get => this._shortValue;
            set {
                this._sbyteValue = unchecked((sbyte)value);
                this._byteValue = unchecked((byte)value);
                this._shortValue = value;
                this._ushortValue = (ushort)(value & 0xFFFF);
                this._intValue = value;
                this._uintValue = (uint)(value & 0xFFFF);
                this._longValue = value;
                this._ulongValue = (ulong)(value & 0xFFFF);
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.Short;
            }
        }

        /// <summary>
        /// Represents the numeric value as an unsigned short.
        /// </summary>
        public ushort UShortValue {
            get => this._ushortValue;
            set {
                this._sbyteValue = unchecked((sbyte)value);
                this._byteValue = unchecked((byte)value);
                this._shortValue = unchecked((short)value);
                this._ushortValue = value;
                this._intValue = value;
                this._uintValue = value;
                this._longValue = value;
                this._ulongValue = value;
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.UShort;
            }
        }

        /// <summary>
        /// Represents the numeric value as an int.
        /// </summary>
        public int IntValue {
            get => this._intValue;
            set {
                this._sbyteValue = unchecked((sbyte)value);
                this._byteValue = unchecked((byte)value);
                this._shortValue = unchecked((short)value);
                this._ushortValue = unchecked((ushort)value);
                this._intValue = value;
                this._uintValue = unchecked((uint)(value & 0xFFFFFFFFU));
                this._longValue = value;
                this._ulongValue = (ulong)(value & 0xFFFFFFFFU);
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.Int;
            }
        }

        /// <summary>
        /// Represents the numeric value as an unsigned int.
        /// </summary>
        public uint UIntValue {
            get => this._uintValue;
            set {
                unchecked {
                    this._sbyteValue = (sbyte)value;
                    this._byteValue = (byte)value;
                    this._shortValue = (short)value;
                    this._ushortValue = (ushort)value;
                    this._intValue = (int)value;
                }

                this._uintValue = value;
                this._longValue = value;
                this._ulongValue = value;
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.UInt;
            }
        }

        /// <summary>
        /// Represents the numeric value as a long.
        /// </summary>
        public long LongValue {
            get => this._longValue;
            set {
                unchecked {
                    this._sbyteValue = (sbyte)value;
                    this._byteValue = (byte)value;
                    this._shortValue = (short)value;
                    this._ushortValue = (ushort)value;
                    this._intValue = (int)value;
                    this._uintValue = (uint)value;
                }

                this._longValue = value;
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this._ulongValue = unchecked((ulong)value);
                this.Type = AbstractNumberType.Long;
            }
        }

        /// <summary>
        /// Represents the numeric value as an unsigned long.
        /// </summary>
        public ulong ULongValue {
            get => this._ulongValue;
            set {
                unchecked {
                    this._sbyteValue = (sbyte)value;
                    this._byteValue = (byte)value;
                    this._shortValue = (short)value;
                    this._ushortValue = (ushort)value;
                    this._intValue = (int)value;
                    this._uintValue = (uint)value;
                    this._longValue = (long)value;
                }

                this._ulongValue = value;
                this._halfValue = (Half)(float)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.ULong;
            }
        }
        
        /// <summary>
        /// Represents the numeric value as a float.
        /// </summary>
        public Half HalfValue {
            get => this._halfValue;
            set {
                unchecked {
                    long longValue = (long)(float)value; // If this is outside of the range of a long, this is undefined behavior. I don't think there's really a way to handle this though.
                    this._sbyteValue = (sbyte)longValue;
                    this._byteValue = (byte)longValue;
                    this._shortValue = (short)longValue;
                    this._ushortValue = (ushort)longValue;
                    this._intValue = (int)longValue;
                    this._uintValue = (uint)longValue;
                    this._longValue = longValue;
                    this._ulongValue = (ulong)(float)value;
                }

                this._halfValue = value;
                this._floatValue = (float)value;
                this._doubleValue = (double)value;
                this.Type = AbstractNumberType.Half;
            }
        }

        /// <summary>
        /// Represents the numeric value as a float.
        /// </summary>
        public float FloatValue {
            get => this._floatValue;
            set {
                unchecked {
                    long longValue = (long)value; // If this is outside of the range of a long, this is undefined behavior. I don't think there's really a way to handle this though.
                    this._sbyteValue = (sbyte)longValue;
                    this._byteValue = (byte)longValue;
                    this._shortValue = (short)longValue;
                    this._ushortValue = (ushort)longValue;
                    this._intValue = (int)longValue;
                    this._uintValue = (uint)longValue;
                    this._longValue = longValue;
                    this._ulongValue = (ulong)value;
                }

                this._halfValue = (Half)value;
                this._floatValue = value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.Float;
            }
        }

        /// <summary>
        /// Represents the numeric value as a double.
        /// </summary>
        public double DoubleValue {
            get => this._doubleValue;
            set {
                unchecked {
                    long longValue = (long)value; // If this is outside of the range of a long, this is undefined behavior. I don't think there's really a way to handle this though.
                    this._sbyteValue = (sbyte)longValue;
                    this._byteValue = (byte)longValue;
                    this._shortValue = (short)longValue;
                    this._ushortValue = (ushort)longValue;
                    this._intValue = (int)longValue;
                    this._uintValue = (uint)longValue;
                    this._longValue = longValue;
                    this._ulongValue = (ulong)value;
                }

                this._halfValue = (Half)(float)value;
                this._floatValue = (float)value;
                this._doubleValue = value;
                this.Type = AbstractNumberType.Double;
            }
        }

        /// <summary>
        /// Returns a decimal containing the value of the current value, from its proper type.
        /// </summary>
        public decimal Value {
            get {
                return this.Type switch {
                    AbstractNumberType.SByte => this._sbyteValue,
                    AbstractNumberType.Byte => this._byteValue,
                    AbstractNumberType.Short => this._shortValue,
                    AbstractNumberType.UShort => this._ushortValue,
                    AbstractNumberType.Int => this._intValue,
                    AbstractNumberType.UInt => this._uintValue,
                    AbstractNumberType.Long => this._longValue,
                    AbstractNumberType.ULong => this._ulongValue,
                    AbstractNumberType.Half => (decimal)(float)this._halfValue,
                    AbstractNumberType.Float => (decimal)this._floatValue,
                    AbstractNumberType.Double => (decimal)this._doubleValue,
                    _ => throw new InvalidOperationException($"Cannot get value of type '{this.Type.CalculateName()}'.")
                };
            }
        }

        // Private constructor.
        private AbstractNumber() {
        }

        public AbstractNumber(sbyte value) {
            this.SByteValue = value;
        }

        public AbstractNumber(byte value) {
            this.ByteValue = value;
        }

        public AbstractNumber(short value) {
            this.ShortValue = value;
        }

        public AbstractNumber(ushort value) {
            this.UShortValue = value;
        }

        public AbstractNumber(int value) {
            this.IntValue = value;
        }

        public AbstractNumber(uint value) {
            this.UIntValue = value;
        }

        public AbstractNumber(long value) {
            this.LongValue = value;
        }

        public AbstractNumber(ulong value) {
            this.ULongValue = value;
        }
        
        public AbstractNumber(Half value) {
            this.HalfValue = value;
        }

        public AbstractNumber(float value) {
            this.FloatValue = value;
        }

        public AbstractNumber(double value) {
            this.DoubleValue = value;
        }

        /// <summary>
        /// Cast this number to another numeric type.
        /// </summary>
        /// <param name="newType">The type to cast this number to.</param>
        /// <param name="createNew">Whether or not a new number object should be created. Defaults to true.</param>
        /// <returns></returns>
        public AbstractNumber Cast(AbstractNumberType newType, bool createNew = true) {
            if (createNew) {
                AbstractNumber newNumber = this.Clone();
                newNumber.Type = newType;
                return newNumber;
            } else {
                this.Type = newType;
                return this;
            }
        }

        /// <summary>
        /// Adds another number to this one, applying the results.
        /// </summary>
        /// <param name="other">The number to add.</param>
        /// <param name="updateType">Whether or not to update the number type after performing the calculation.</param>
        public void Add(AbstractNumber other, bool updateType = false) {
            decimal otherNumber = other.Value;
            this._sbyteValue = (sbyte)((long)(this._sbyteValue + otherNumber) & 0xFF);
            this._byteValue = (byte)((long)(this._byteValue + otherNumber) & 0xFF);
            this._shortValue = (short)((long)(this._shortValue + otherNumber) & 0xFFFF);
            this._ushortValue = (ushort)((long)(this._ushortValue + otherNumber) & 0xFFFF);
            this._intValue = (int)((long)(this._intValue + otherNumber) & 0xFFFFFFFF);
            this._uintValue = (uint)((long)(this._uintValue + otherNumber) & 0xFFFFFFFF);
            this._longValue = ((long)(this._longValue + otherNumber));
            this._ulongValue = unchecked((ulong)this._longValue); // Doing it this way avoids overflow exceptions.
            this._halfValue = (Half)((float)this._halfValue + (float)otherNumber);
            this._floatValue = (float)(this._floatValue + (double)otherNumber);
            this._doubleValue = (this._doubleValue + (double)otherNumber);
            if (updateType)
                this.UpdateType(other.Type);
        }

        /// <summary>
        /// Subtracts another number from this one, applying the results.
        /// </summary>
        /// <param name="other">The number to subtract.</param>
        /// <param name="updateType">Whether or not to update the number type after performing the calculation.</param>
        public void Subtract(AbstractNumber other, bool updateType = false) {
            decimal otherNumber = other.Value;
            this._sbyteValue = (sbyte)((long)(this._sbyteValue - otherNumber) & 0xFF);
            this._byteValue = (byte)((long)(this._byteValue - otherNumber) & 0xFF);
            this._shortValue = (short)((long)(this._shortValue - otherNumber) & 0xFFFF);
            this._ushortValue = (ushort)((long)(this._ushortValue - otherNumber) & 0xFFFF);
            this._intValue = (int)((long)(this._intValue - otherNumber) & 0xFFFFFFFF);
            this._uintValue = (uint)((long)(this._uintValue - otherNumber) & 0xFFFFFFFF);
            this._longValue = ((long)(this._longValue - otherNumber));
            this._ulongValue = unchecked((ulong)this._longValue); // Doing it this way avoids overflow exceptions.
            this._halfValue = (Half)((float)this._halfValue - (float)otherNumber);
            this._floatValue = (float)(this._floatValue - (double)otherNumber);
            this._doubleValue = (this._doubleValue - (double)otherNumber);
            if (updateType)
                this.UpdateType(other.Type);
        }

        /// <summary>
        /// Multiplies this number by another number, applying the results.
        /// </summary>
        /// <param name="other">The number to multiply this number by.</param>
        /// <param name="updateType">Whether or not to update the number type after performing the calculation.</param>
        public void Multiply(AbstractNumber other, bool updateType = false) {
            decimal otherNumber = other.Value;
            this._sbyteValue = (sbyte)((int)(this._sbyteValue * otherNumber) & 0xFF);
            this._byteValue = (byte)((int)(this._byteValue * otherNumber) & 0xFF);
            this._shortValue = (short)((int)(this._shortValue * otherNumber) & 0xFFFF);
            this._ushortValue = (ushort)((int)(this._ushortValue * otherNumber) & 0xFFFF);
            this._intValue = (int)((long)(this._intValue * otherNumber) & 0xFFFFFFFF);
            this._uintValue = (uint)((long)(this._uintValue * otherNumber) & 0xFFFFFFFF);
            this._longValue = ((long)(this._longValue * otherNumber));
            this._ulongValue = unchecked((ulong)this._longValue); // I did some bitwise experimentation and it seems applying the operation on the signed value will indeed result in the same bits as the unsigned value. Doing it this way avoids overflow exceptions.
            this._halfValue = (Half)((float)this._halfValue * (float)otherNumber);
            this._floatValue = (float)(this._floatValue * (double)otherNumber);
            this._doubleValue = (this._doubleValue * (double)otherNumber);
            if (updateType)
                this.UpdateType(other.Type);
        }

        /// <summary>
        /// Divides this number by another number, applying the results.
        /// </summary>
        /// <param name="other">The number to divide this number by.</param>
        /// <param name="updateType">Whether or not to update the number type after performing the calculation.</param>
        public void DivideBy(AbstractNumber other, bool updateType = false) {
            decimal otherNumber = other.Value;
            this._sbyteValue = (sbyte)((int)(this._sbyteValue / otherNumber) & 0xFF);
            this._byteValue = (byte)((int)(this._byteValue / otherNumber) & 0xFF);
            this._shortValue = (short)((int)(this._shortValue / otherNumber) & 0xFFFF);
            this._ushortValue = (ushort)((int)(this._ushortValue / otherNumber) & 0xFFFF);
            this._intValue = (int)((long)(this._intValue / otherNumber) & 0xFFFFFFFF);
            this._uintValue = (uint)((long)(this._uintValue / otherNumber) & 0xFFFFFFFF);
            this._longValue = ((long)(this._longValue / otherNumber));
            this._ulongValue = unchecked((ulong)this._longValue); // I did some bitwise experimentation and it seems applying the operation on the signed value will indeed result in the same bits as the unsigned value. Doing it this way avoids overflow exceptions.
            this._halfValue = (Half)((float)this._halfValue / (float)otherNumber);
            this._floatValue = (float)(this._floatValue / (double)otherNumber);
            this._doubleValue = (this._doubleValue / (double)otherNumber);
            if (updateType)
                this.UpdateType(other.Type);
        }

        /// <summary>
        /// Calculates a new type to consider the value as.
        /// </summary>
        private void UpdateType(AbstractNumberType otherType) {
            // Turn number into a floating-point value.
            if (this.Type.IsFloatingPoint() || otherType.IsFloatingPoint()) {
                bool useDouble = (this.Type == AbstractNumberType.Double || otherType == AbstractNumberType.Double);
                this.Type = useDouble ? AbstractNumberType.Double : AbstractNumberType.Float;
                return;
            }

            if (this.Type.IsSignedInteger() != otherType.IsSignedInteger())
                throw new InvalidOperationException($"Signed numbers cannot be operated on with type-updates with unsigned numbers! ({this.Type.CalculateName()}, {otherType.CalculateName()})");

            // Become the other type if it is larger.
            if (otherType.GetByteSize() > this.Type.GetByteSize())
                this.Type = otherType;
        }

        /// <summary>
        /// Creates a copy of this AbstractNumber.
        /// </summary>
        /// <returns>clonedNumberObject</returns>
        public AbstractNumber Clone() {
            AbstractNumber newNumber = new AbstractNumber();
            newNumber._sbyteValue = this._sbyteValue;
            newNumber._byteValue = this._byteValue;
            newNumber._shortValue = this._shortValue;
            newNumber._ushortValue = this._ushortValue;
            newNumber._intValue = this._intValue;
            newNumber._uintValue = this._uintValue;
            newNumber._longValue = this._longValue;
            newNumber._ulongValue = this._ulongValue;
            newNumber._halfValue = this._halfValue;
            newNumber._floatValue = this._floatValue;
            newNumber._doubleValue = this._doubleValue;
            newNumber.Type = this.Type;
            return newNumber;
        }

        /// <summary>
        /// Gets the number as a string, encoded in a particular way.
        /// </summary>
        /// <param name="encodingType">The string encoding method to use.</param>
        /// <param name="includeSuffix">Whether or not to include type suffixes like "L" or "U".</param>
        /// <returns>numberString</returns>
        public string GetNumberAsString(NumberStringEncoding encodingType = NumberStringEncoding.Normal, bool includeSuffix = false) {
            StringBuilder builder = new StringBuilder();
            this.GetNumberAsString(builder, encodingType, includeSuffix);
            return builder.ToString();
        }

        /// <summary>
        /// Gets the number as a string, encoded in a particular way.
        /// </summary>
        /// <param name="builder">The builder to write the string to.</param>
        /// <param name="encodingType">The string encoding method to use.</param>
        /// <param name="includeSuffix">Whether or not to include type suffixes like "L" or "U".</param>
        public void GetNumberAsString(StringBuilder builder, NumberStringEncoding encodingType = NumberStringEncoding.Normal, bool includeSuffix = false) {
            bool isFloat = this.Type.IsFloatingPoint();

            // This bit mask is used to allow us to only deal with one integer number.
            decimal value = this.Value;

            long signedNumberValue = 0;
            ulong unsignedNumberValue = 0;
            if (!isFloat) {
                unchecked {
                    signedNumberValue = (long)value;
                    unsignedNumberValue = (ulong)signedNumberValue;
                }
            }

            switch (encodingType) {
                case NumberStringEncoding.Binary:
                    if (isFloat)
                        throw new InvalidOperationException("Floating point numbers cannot be printed in binary!");
                    builder.Append("0b");
                    int byteSize = this.Type.GetByteSize();
                    if (byteSize == DataConstants.ByteSize) {
                        builder.Append(Convert.ToString((byte)signedNumberValue, 2));
                    } else if (byteSize == DataConstants.ShortSize) {
                        builder.Append(Convert.ToString((short)signedNumberValue, 2));
                    } else if (byteSize == DataConstants.IntegerSize) {
                        builder.Append(Convert.ToString((int)signedNumberValue, 2));
                    } else {
                        builder.Append(Convert.ToString(signedNumberValue, 2));
                    }

                    break;
                case NumberStringEncoding.Normal:
                    if (this.Type == AbstractNumberType.Double) {
                        builder.Append(this._doubleValue.ToString("N100", CultureInfo.InvariantCulture));
                    } else if (this.Type == AbstractNumberType.Float) {
                        builder.Append(this._floatValue.ToString("N100", CultureInfo.InvariantCulture));
                    } else if (this.Type.IsSignedInteger()) {
                        builder.Append(signedNumberValue);
                    } else if (this.Type.IsUnsignedInteger()) {
                        builder.Append(unsignedNumberValue);
                    } else {
                        throw new InvalidOperationException($"Cannot encode number with unhandled type! ({this.Type.CalculateName()})");
                    }

                    break;
                case NumberStringEncoding.Hex:
                    if (isFloat)
                        throw new InvalidOperationException("Floating point numbers cannot be printed in hex!");
                    builder.Append("0x");
                    int byteSize2 = this.Type.GetByteSize();
                    if (byteSize2 == DataConstants.ByteSize) {
                        builder.Append(Convert.ToString((byte)signedNumberValue, 16).ToUpperInvariant());
                    } else if (byteSize2 == DataConstants.ShortSize) {
                        builder.Append(Convert.ToString((short)signedNumberValue, 16).ToUpperInvariant());
                    } else if (byteSize2 == DataConstants.IntegerSize) {
                        builder.Append(Convert.ToString((int)signedNumberValue, 16).ToUpperInvariant());
                    } else {
                        builder.Append(Convert.ToString(signedNumberValue, 16).ToUpperInvariant());
                    }

                    break;
                case NumberStringEncoding.ScientificNotation:
                    if (!isFloat)
                        throw new InvalidOperationException($"Cannot encode {this.Type.CalculateName()} with scientific notation, only floating point numbers are supported!");
                    if (this.Type == AbstractNumberType.Double) {
                        builder.Append(this._doubleValue.ToString("E", CultureInfo.InvariantCulture));
                    } else if (this.Type == AbstractNumberType.Float) {
                        builder.Append(this._floatValue.ToString("E", CultureInfo.InvariantCulture));
                    } else {
                        throw new InvalidOperationException($"Cannot encode number with unhandled type! ({this.Type.CalculateName()})");
                    }

                    break;
                default:
                    throw new InvalidOperationException("Cannot encode number with unknown encoding type!");
            }

            if (includeSuffix && encodingType != NumberStringEncoding.ScientificNotation) {
                if (this.Type == AbstractNumberType.Double) {
                    builder.Append('D');
                } else if (this.Type == AbstractNumberType.Float) {
                    builder.Append('F');
                } else if (this.Type == AbstractNumberType.Half) {
                    builder.Append('H');
                } else {
                    if (this.Type.IsUnsignedInteger())
                        builder.Append('U');
                    if (this.Type == AbstractNumberType.Long || this.Type == AbstractNumberType.ULong)
                        builder.Append('L');
                }
            }
        }

        /// <inheritdoc cref="System.Object"/>
        public override bool Equals(object obj) {
            return (obj is AbstractNumber other) && other.Type == this.Type && other.Value == this.Value;
        }

        /// <inheritdoc cref="System.Object"/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() {
            return this._intValue; // Really you shouldn't be getting the hashcode of this, but I suppose it doesn't hurt to half-way support it if someone tries?
        }

        /// <inheritdoc cref="System.Object"/>
        public override string ToString() {
            StringBuilder builder = new StringBuilder();
            builder.Append("Number{");
            builder.Append(this.Type.GetName().ToLowerInvariant());
            builder.Append('=');
            builder.Append(this.GetNumberAsString());
            builder.Append(' ');
            if (this.Type.IsInteger())
                builder.Append(this.GetNumberAsString(NumberStringEncoding.Hex, true));

            return builder.Append('}').ToString();
        }

        /// <summary>
        /// Adds the second number to the first number, returning a new number with a calculated type.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Resulting number</returns>
        public static AbstractNumber operator +(AbstractNumber a, AbstractNumber b) {
            AbstractNumber newNumber = a.Clone();
            newNumber.Add(b, true);
            return newNumber;
        }

        /// <summary>
        /// Subtracts the second number from the first number, returning a new number with a calculated type.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Resulting number</returns>
        public static AbstractNumber operator -(AbstractNumber a, AbstractNumber b) {
            AbstractNumber newNumber = a.Clone();
            newNumber.Subtract(b, true);
            return newNumber;
        }

        /// <summary>
        /// Multiplies the first number by the second number, returning a new number with a calculated type.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Resulting number</returns>
        public static AbstractNumber operator *(AbstractNumber a, AbstractNumber b) {
            AbstractNumber newNumber = a.Clone();
            newNumber.Multiply(b, true);
            return newNumber;
        }

        /// <summary>
        /// Divides the first number by the second number, returning a new number with a calculated type.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>Resulting number</returns>
        public static AbstractNumber operator /(AbstractNumber a, AbstractNumber b) {
            AbstractNumber newNumber = a.Clone();
            newNumber.DivideBy(b, true);
            return newNumber;
        }

        /// <summary>
        /// Test if one number is greater than or equal to another number.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>operationResult</returns>
        public static bool operator >=(AbstractNumber a, AbstractNumber b) {
            return (a.Value >= b.Value);
        }

        /// <summary>
        /// Test if one number is greater than another number.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>operationResult</returns>
        public static bool operator >(AbstractNumber a, AbstractNumber b) {
            return (a.Value > b.Value);
        }

        /// <summary>
        /// Test if one number is less than or equal to another number.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>operationResult</returns>
        public static bool operator <=(AbstractNumber a, AbstractNumber b) {
            return (a.Value <= b.Value);
        }

        /// <summary>
        /// Test if one number is less than another number.
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>operationResult</returns>
        public static bool operator <(AbstractNumber a, AbstractNumber b) {
            return (a.Value < b.Value);
        }

        /// <summary>
        /// Tests if a given string can be read as an AbstractNumber.
        /// </summary>
        /// <param name="numberText">The text to test.</param>
        /// <returns>CanBeParsed</returns>
        public static bool IsValidNumber(string numberText) {
            using EnhancedStringReader reader = new EnhancedStringReader(numberText);
            return IsValidNumber(reader);
        }

        /// <summary>
        /// Tests if a number is a valid 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static bool IsValidNumber(EnhancedStringReader reader) {
            bool isHex = false;
            bool isBinary = false;
            bool isDecimalSeen = false;
            bool isUnsigned = false;
            bool canBeHex = true;
            bool canBeFloat = true;
            bool canBeLong = true;
            bool canHaveMoreDigits = true;
            bool lastWasScientific = false;
            bool isNegative = false;
            bool hasNumericValue = false;

            char nextChar = '\0';
            int readChars = -1;
            while (reader.HasMore) {
                readChars++;
                char lastChar = nextChar;
                nextChar = reader.ReadChar();

                if (lastWasScientific) {
                    lastWasScientific = false;
                } else if (canBeFloat && nextChar == '.') {
                    canBeHex = false;
                    canBeLong = false;
                    if (isDecimalSeen) // Not the first decimal we've found, exit.
                        return false;
                    isDecimalSeen = true;
                    hasNumericValue = false;
                } else if (canHaveMoreDigits && isHex && GeneralUtils.IsHexadecimal(nextChar)) {
                    hasNumericValue = true;
                } else if (canHaveMoreDigits && isBinary && (nextChar == '0' || nextChar == '1')) {
                    hasNumericValue = true;
                } else if (canHaveMoreDigits && !isHex && !isBinary && GeneralUtils.IsDigit(nextChar)) {
                    hasNumericValue = true;
                } else if (nextChar == '-' && readChars == 0) {
                    isNegative = true;
                } else if (canBeHex && (readChars == (isNegative ? 2 : 1)) && lastChar == '0' && (nextChar == 'b' || nextChar == 'x')) {
                    canBeHex = false;
                    canBeFloat = false;
                    hasNumericValue = false; // '0' in '0b' or '0x' does not count.
                    if (nextChar == 'b') {
                        isBinary = true;
                    } else if (nextChar == 'x') {
                        isHex = true;
                    }
                } else if (canBeFloat && (nextChar == 'F' || nextChar == 'f' || nextChar == 'D' || nextChar == 'd' || nextChar == 'h' || nextChar == 'H')) {
                    break;
                } else if (canBeLong && nextChar == 'L') {
                    canBeHex = false;
                    canBeFloat = false;
                    canHaveMoreDigits = false;
                    nextChar = '\0';
                } else if (!isDecimalSeen && nextChar == 'U') {
                    if (isUnsigned)
                        return false; // Number declared as unsigned twice!

                    isUnsigned = true;
                    canHaveMoreDigits = false;
                    canBeHex = false;
                    canBeFloat = false;
                    nextChar = '\0';
                } else if (canBeFloat && nextChar == 'e') {
                    lastWasScientific = true;
                    canBeHex = false;
                    canBeLong = false;
                    canBeFloat = false;
                } else {
                    if (!GeneralUtils.IsWhitespace(nextChar))
                        return false; // Unexpected character.
                    break;
                }
            }

            if (reader.HasMore)
                reader.Index--;

            if (isUnsigned && isNegative)
                return false; // Number cannot be both negative and unsigned!
            
            return hasNumericValue;
        }
        
        /// <summary>
        /// Parses a number from a string.
        /// </summary>
        /// <param name="inputStr">The string to read as a number.</param>
        /// <returns>parsedNumber</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the number syntax is invalid.</exception>
        public static AbstractNumber ParseNumber(string inputStr) {
            return ParseNumber(inputStr, out NumberStringEncoding _);
        }

        /// <summary>
        /// Parses a number from a string.
        /// </summary>
        /// <param name="inputStr">The string to read as a number.</param>
        /// <param name="encodingType">The variable to store the encoding type in.</param>
        /// <returns>parsedNumber</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the number syntax is invalid.</exception>
        public static AbstractNumber ParseNumber(string inputStr, out NumberStringEncoding encodingType) {
            using EnhancedStringReader reader = new EnhancedStringReader(inputStr);
            return ParseNumber(reader, out encodingType);
        }

        /// <summary>
        /// Parses a number from a string reader.
        /// </summary>
        /// <param name="reader">The reader to read the number from.</param>
        /// <returns>parsedNumber</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the number syntax is invalid.</exception>
        public static AbstractNumber ParseNumber(EnhancedStringReader reader) {
            return ParseNumber(reader, out NumberStringEncoding _);
        }

        /// <summary>
        /// Parses a number from a string reader.
        /// </summary>
        /// <param name="reader">The reader to read the number from.</param>
        /// <param name="encodingType">The variable to store the encoding type in.</param>
        /// <returns>parsedNumber</returns>
        /// <exception cref="SyntaxErrorException">Thrown if the number syntax is invalid.</exception>
        public static AbstractNumber ParseNumber(EnhancedStringReader reader, out NumberStringEncoding encodingType) {
            bool isHex = false;
            bool isBinary = false;
            bool isHalfMarker = false;
            bool isFloatMarker = false;
            bool isDoubleMarker = false;
            bool isLongMarker = false;
            bool isScienceMarker = false;
            bool isDecimalSeen = false;
            bool needDigitsAfterDecimal = false;
            bool isUnsigned = false;
            bool canBeHex = true;
            bool canBeFloat = true;
            bool canBeLong = true;
            bool canHaveMoreDigits = true;
            bool lastWasScientific = false;
            bool isNegative = false;
            bool hasNumericValue = false;

            char nextChar = '\0';
            StringBuilder numBuilder = new StringBuilder();
            while (reader.HasMore) {
                if (nextChar != '\0')
                    numBuilder.Append(nextChar);

                char lastChar = nextChar;
                nextChar = reader.ReadChar();
                if (lastWasScientific) {
                    if (nextChar != '+' && nextChar != '-') {
                        nextChar = '+';
                        reader.Index--;
                    }

                    lastWasScientific = false;
                } else if (canBeFloat && nextChar == '.') {
                    canBeHex = false;
                    canBeLong = false;
                    if (isDecimalSeen) // Not the first decimal we've found, exit.
                        throw new SyntaxErrorException("Found two decimal splits in a single numeric token");
                    isDecimalSeen = true;
                    needDigitsAfterDecimal = true;
                } else if (canHaveMoreDigits && isHex && GeneralUtils.IsHexadecimal(nextChar)) {
                    hasNumericValue = true;
                } else if (canHaveMoreDigits && isBinary && (nextChar == '0' || nextChar == '1')) {
                    hasNumericValue = true;
                } else if (canHaveMoreDigits && !isHex && !isBinary && GeneralUtils.IsDigit(nextChar)) {
                    hasNumericValue = true;
                    needDigitsAfterDecimal = false;
                } else if (nextChar == '-' && numBuilder.Length == 0) {
                    isNegative = true;
                } else if (canBeHex && (numBuilder.Length == (isNegative ? 2 : 1)) && lastChar == '0' && (nextChar == 'b' || nextChar == 'x')) {
                    canBeHex = false;
                    canBeFloat = false;
                    if (nextChar == 'b') {
                        isBinary = true;
                    } else if (nextChar == 'x') {
                        isHex = true;
                    }
                    hasNumericValue = false;
                } else if (canBeFloat && (nextChar == 'F' || nextChar == 'f' || nextChar == 'D' || nextChar == 'd' || nextChar == 'h' || nextChar == 'H')) {
                    if (nextChar == 'F' || nextChar == 'f') {
                        isFloatMarker = true;
                    } else if (nextChar == 'D' || nextChar == 'd') {
                        isDoubleMarker = true;
                    } else if (nextChar == 'H' || nextChar == 'h') {
                        isHalfMarker = true;
                    }

                    //canBeFloat = false;
                    //canBeHex = false;
                    //canBeLong = false;
                    nextChar = '\0';
                    break;
                } else if (canBeLong && nextChar == 'L') {
                    canBeHex = false;
                    canBeFloat = false;
                    canHaveMoreDigits = false;
                    isLongMarker = true;
                    nextChar = '\0';
                } else if (!isDecimalSeen && nextChar == 'U') {
                    if (isUnsigned)
                        throw new SyntaxErrorException("Number declared as unsigned twice!");

                    isUnsigned = true;
                    canHaveMoreDigits = false;
                    canBeHex = false;
                    canBeFloat = false;
                    nextChar = '\0';
                } else if (canBeFloat && nextChar == 'e') {
                    lastWasScientific = true;
                    isScienceMarker = true;
                    canBeHex = false;
                    canBeLong = false;
                    canBeFloat = false;
                } else {
                    if (!GeneralUtils.IsWhitespace(nextChar))
                        throw new SyntaxErrorException($"Unexpected character in number '{nextChar}'.");
                    nextChar = '\0';
                    break;
                }
            }

            if (nextChar != '\0')
                numBuilder.Append(nextChar);

            if (reader.HasMore)
                reader.Index--;

            string numberStr = numBuilder.ToString();
            if (needDigitsAfterDecimal)
                throw new SyntaxErrorException($"There was no number after the decimal point in '{numberStr}'.");
            if (!hasNumericValue)
                throw new SyntaxErrorException($"There is no numeric value in the string '{numberStr}'.");
            if (isUnsigned && isNegative)
                throw new SyntaxErrorException($"Number '{numberStr}' cannot be both negative and unsigned!");
            
            if (isHex || isBinary)
                numberStr = numberStr[(isNegative ? 3 : 2)..];
            
            if (isHex) {
                encodingType = NumberStringEncoding.Hex;
            } else if (isBinary) {
                encodingType = NumberStringEncoding.Binary;
            } else if (isScienceMarker) {
                encodingType = NumberStringEncoding.ScientificNotation;
            } else {
                encodingType = NumberStringEncoding.Normal;
            }

            if (isHalfMarker) {
                return new AbstractNumber((Half)(float)Convert.ToDouble(numberStr, CultureInfo.InvariantCulture));
            } else if (isFloatMarker) {
                return new AbstractNumber((float)Convert.ToDouble(numberStr, CultureInfo.InvariantCulture));
            } else if (isDoubleMarker || isDecimalSeen || isScienceMarker) {
                return new AbstractNumber(Convert.ToDouble(numberStr, CultureInfo.InvariantCulture));
            } else {
                if (isLongMarker) {
                    if (isUnsigned) {
                        if (isHex) {
                            return new AbstractNumber(Convert.ToUInt64(numberStr, 16));
                        } else if (isBinary) {
                            return new AbstractNumber(Convert.ToUInt64(numberStr, 2));
                        } else {
                            return new AbstractNumber(Convert.ToUInt64(numberStr, 10));
                        }
                    } else {
                        if (isHex) {
                            return new AbstractNumber((isNegative ? -1L : 1L) * Convert.ToInt64(numberStr, 16));
                        } else if (isBinary) {
                            return new AbstractNumber((isNegative ? -1L : 1L) * Convert.ToInt64(numberStr, 2));
                        } else {
                            return new AbstractNumber(Convert.ToInt64(numberStr, 10));
                        }
                    }
                } else {
                    if (isUnsigned) {
                        if (isHex) {
                            return new AbstractNumber(Convert.ToUInt32(numberStr, 16));
                        } else if (isBinary) {
                            return new AbstractNumber(Convert.ToUInt32(numberStr, 2));
                        } else {
                            return new AbstractNumber(Convert.ToUInt32(numberStr, 10));
                        }
                    } else {
                        if (isHex) {
                            return new AbstractNumber((isNegative ? -1 : 1) * Convert.ToInt32(numberStr, 16));
                        } else if (isBinary) {
                            return new AbstractNumber((isNegative ? -1 : 1) * Convert.ToInt32(numberStr, 2));
                        } else {
                            return new AbstractNumber(Convert.ToInt32(numberStr, 10));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A registry of the different ways to encoding a numeric string.
    /// </summary>
    public enum NumberStringEncoding {
        Binary, // Base 2
        Normal, // Base 10
        Hex, // Base 16
        ScientificNotation // Base 10.
    }

    /// <summary>
    /// A registry of all of the different types that an abstract number could be.
    /// </summary>
    public enum AbstractNumberType {
        SByte,
        Byte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Half,
        Float,
        Double
    }

    public static class AbstractNumberTypeExtensions {
        /// <summary>
        /// Tests whether or not an AbstractNumberType is an unsigned integer type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>IsUnsignedInteger</returns>
        public static bool IsUnsignedInteger(this AbstractNumberType type) {
            return type == AbstractNumberType.Byte || type == AbstractNumberType.UShort
                || type == AbstractNumberType.UInt || type == AbstractNumberType.ULong;
        }

        /// <summary>
        /// Tests whether or not an AbstractNumberType is a signed integer type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>IsSignedInteger</returns>
        public static bool IsSignedInteger(this AbstractNumberType type) {
            return type == AbstractNumberType.SByte || type == AbstractNumberType.Short
                || type == AbstractNumberType.Int || type == AbstractNumberType.Long;
        }

        /// <summary>
        /// Tests whether or not an AbstractNumberType is an integer type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>IsInteger</returns>
        public static bool IsInteger(this AbstractNumberType type) {
            return !type.IsFloatingPoint();
        }

        /// <summary>
        /// Tests whether or not an AbstractNumberType is a signed number type.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>IsSignedNumberType</returns>
        public static bool IsSigned(this AbstractNumberType type) {
            return type.IsFloatingPoint() || type.IsSignedInteger();
        }

        /// <summary>
        /// Tests whether or not an AbstractNumberType is a floating point number.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>IsFloatingPoint</returns>
        public static bool IsFloatingPoint(this AbstractNumberType type) {
            return (type == AbstractNumberType.Half) || (type == AbstractNumberType.Float) || (type == AbstractNumberType.Double);
        }

        /// <summary>
        /// Gets the number of bytes which the number type takes up.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>ByteSize</returns>
        public static int GetByteSize(this AbstractNumberType type) {
            switch (type) {
                case AbstractNumberType.Byte:
                case AbstractNumberType.SByte:
                    return DataConstants.ByteSize;
                case AbstractNumberType.Short:
                case AbstractNumberType.UShort:
                    return DataConstants.ShortSize;
                case AbstractNumberType.Int:
                case AbstractNumberType.UInt:
                    return DataConstants.IntegerSize;
                case AbstractNumberType.Long:
                case AbstractNumberType.ULong:
                    return DataConstants.LongSize;
                case AbstractNumberType.Half:
                    return DataConstants.HalfSize;
                case AbstractNumberType.Float:
                    return DataConstants.FloatSize;
                case AbstractNumberType.Double:
                    return DataConstants.DoubleSize;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}