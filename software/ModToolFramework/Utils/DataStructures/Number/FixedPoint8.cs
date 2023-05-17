using System;
using System.Diagnostics.CodeAnalysis;

namespace ModToolFramework.Utils.DataStructures.Number
{
    /// <summary>
    /// Represents a signed 8-bit fixed point decimal number.
    /// Fixed point numbers are integers which use some of their bits to represent a decimal number, and some bits to represent the integer number.
    /// Decimal bits are expected to be located on the nth least-significant bits.
    /// </summary>
    [SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
    public struct FixedPoint8
    {
        private byte _decimalBits;

        /// <summary>
        /// Gets the raw sbyte which contains the bits of the fixed point number.
        /// </summary>
        public sbyte FixedValue { get; set; }

        /// <summary>
        /// Accesses the raw byte which contains the bits of the fixed point number.
        /// </summary>
        public byte UnsignedFixedValue {
            get => unchecked((byte)this.FixedValue);
            set => this.FixedValue = unchecked((sbyte)value);
        }
        
        /// <summary>
        /// The number of bits which make up this numeric type.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public const int BitCount = 8;

        private static readonly double[] BitsInversePowerOf2 = new double[BitCount * 2];

        static FixedPoint8() {
            for (int i = 0; i < BitsInversePowerOf2.Length; i++)
                BitsInversePowerOf2[i] = Math.Pow(2, -i);
        }

        /// <summary>
        /// Creates a new <see cref="FixedPoint8"/> value.
        /// </summary>
        /// <param name="value">The value to use as a fixed point number.</param>
        /// <param name="decimalBitCount">The number of decimal bits to use.</param>
        public FixedPoint8(float value, int decimalBitCount) {
            if (!NumberUtils.IsRealNumber(value))
                throw new ArgumentException($"Cannot represent {value} as a fixed point number.", nameof(value));

            // Set default values.
            this.FixedValue = 0;
            this._decimalBits = 0;

            // Apply values.
            this.NumberOfDecimalBits = decimalBitCount;
            this.Value = value;
        }
        
        /// <summary>
        /// Creates a new <see cref="FixedPoint8"/> value.
        /// </summary>
        /// <param name="value">The fixed point value.</param>
        /// <param name="decimalBitCount">The number of decimal bits to use.</param>
        public FixedPoint8(sbyte value, int decimalBitCount) {
            // Set default values.
            this.FixedValue = value;
            this._decimalBits = 0;
            
            this.NumberOfDecimalBits = decimalBitCount; // This runs actual tests.
        }

        /// <summary>
        /// Creates a new <see cref="FixedPoint8"/> value.
        /// </summary>
        /// <param name="value">The fixed point value.</param>
        /// <param name="decimalBitCount">The number of decimal bits to use.</param>
        public FixedPoint8(byte value, int decimalBitCount) : this(unchecked((sbyte)value), decimalBitCount) {
        }

        /// <summary>
        /// Accesses the floating point representation of this number.
        /// </summary>
        public float Value {
            readonly get => NumberUtils.ConvertFixedPointSByteToFloat(this.FixedValue, this._decimalBits);
            set {
                if (!NumberUtils.IsRealNumber(value))
                    throw new ArgumentException($"Cannot represent {value} as a fixed point number.", nameof(value));

                this.FixedValue = NumberUtils.ConvertFloatToFixedPointSByte(value, this._decimalBits);
            }
        }

        /// <summary>
        /// The number of bits used for decimal.
        /// Defaults to zero.
        /// </summary>
        public int NumberOfDecimalBits {
            readonly get => this._decimalBits;
            set {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Number of decimal bits cannot be less than zero. (Got: {value})");
                if (value > BitCount - 1)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Number of decimal bits cannot exceed the total number of bits in the number ({BitCount - 1}). (Got: {value})");
                
                this._decimalBits = (byte)value;
            }
        }

        /// <summary>
        /// The number of bits used for the integer part of the number.
        /// This includes the signed bit, which makes the minimum value one.
        /// Defaults to BitCount.
        /// </summary>
        public int NumberOfIntegerBits {
            readonly get => BitCount - this.NumberOfDecimalBits;
            set {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Number of integer bits cannot be set to less than one. (Got: {value})");
                if (value > BitCount)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Number of integer bits cannot exceed the total number of bits in the number ({BitCount}). (Got: {value})");
                this.NumberOfDecimalBits = (byte)(BitCount - value);
            }
        }
        
        /// <summary>
        /// Tests whether or not this equals another fixed point number.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="other">The other number to test.</param>
        /// <returns>Whether or not the numbers match.</returns>
        public readonly bool Equals(FixedPoint8 other) {
            if (other._decimalBits == this._decimalBits)
                return this.FixedValue == other.FixedValue;

            this.GetBits(out byte integerBits, out byte decimalBits);
            other.GetBits(out byte otherIntegerBits, out byte otherDecimalBits);
            if (integerBits != otherIntegerBits)
                return false; // If the integer part doesn't match, return.

            bool otherHasMoreDecimalBits = (otherDecimalBits > decimalBits);
            int removeBitsFromOther = otherHasMoreDecimalBits ? 0 : (decimalBits - otherDecimalBits);
            int removeBitsFromSelf = otherHasMoreDecimalBits ? (otherDecimalBits - decimalBits) : 0;

            // Verify that all bits which present in both numbers are set.
            return (otherDecimalBits >> removeBitsFromOther) == (decimalBits >> removeBitsFromSelf)
                && ((decimalBits & ((1 << removeBitsFromSelf) - 1)) == 0)
                && ((otherDecimalBits & ((1 << removeBitsFromOther) - 1)) == 0);
        }

        /// <inheritdoc cref="ValueType.ToString"/>
        public override readonly string ToString() {
            return $"Fixed[{this.NumberOfIntegerBits}.{this.NumberOfDecimalBits}]{{Fixed={this.FixedValue},Float={this.Value}}}";
        }

        /// <inheritdoc cref="ValueType.Equals(object?)"/>
        public override readonly bool Equals(object obj) {
            return obj is FixedPoint8 other && this.Equals(other);
        }

        /// <inheritdoc cref="ValueType.GetHashCode"/>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override readonly int GetHashCode() {
            return this._decimalBits | (this.FixedValue << 8);
        }

        /// <summary>
        /// Gets the bits which make up this fixed point number.
        /// Because this is for getting bits, the results are unsigned.
        /// </summary>
        /// <param name="integerBits">Output storage for the integer part of the number.</param>
        /// <param name="decimalBits">Output storage for the decimal bits of the number.</param>
        public readonly void GetBits(out byte integerBits, out byte decimalBits) {
            integerBits = (byte)(this.FixedValue >> this.NumberOfDecimalBits);
            decimalBits = (byte)(this.FixedValue & ((1 << this.NumberOfDecimalBits) - 1));
        }
        
        // This cast is explicit since it can create type-confusion when used in ambiguous circumstances.
        // But this one should probably default to 'sbyte', since this is signed. But, it could be useful to get this as a byte,
        //  so we retain the ability to explicitly cast to it.
        public static explicit operator byte(FixedPoint8 num) => unchecked((byte)num.FixedValue);
        
        // Implicit casts.
        public static implicit operator sbyte(FixedPoint8 num) => num.FixedValue;
        public static implicit operator float(FixedPoint8 num) => num.Value;

        /// <summary>
        /// Adds two fixed point numbers together.
        /// The resulting number will use the settings from the first (left-hand) argument.
        /// </summary>
        /// <param name="numOne">The left-hand number to add.</param>
        /// <param name="numTwo">The right-hand number to add.</param>
        /// <returns>sum</returns>
        public static FixedPoint8 operator +(FixedPoint8 numOne, FixedPoint8 numTwo) {
            if (numOne._decimalBits == numTwo._decimalBits) { // If the settings match, fast addition can be done.
                numOne.FixedValue += numTwo.FixedValue;
                return numOne; // Keep settings.
            }

            // Otherwise, use the settings from the first one and keep the result.
            numOne.Value = (numOne.Value + numTwo.Value);
            return numOne; // Keep the settings from #1.
        }
        
        /// <summary>
        /// Adds a fixed point number together with a float.
        /// The resulting number will be fixed-point and use settings from the original fixed point number.
        /// </summary>
        /// <param name="numOne">The fixed-point number to add.</param>
        /// <param name="numTwo">The float number to add.</param>
        /// <returns>sum</returns>
        public static FixedPoint8 operator +(FixedPoint8 numOne, float numTwo) {
            numOne.Value = (numOne.Value + numTwo);
            return numOne; // Keep the settings.
        }
        
        /// <summary>
        /// Adds a fixed point number together with a float.
        /// The resulting number will be a float.
        /// </summary>
        /// <param name="numOne">The float number to add.</param>
        /// <param name="numTwo">The fixed-point number to add.</param>
        /// <returns>sum</returns>
        public static float operator +(float numOne, FixedPoint8 numTwo) {
            return numOne + numTwo.Value;
        }
        
        /// <summary>
        /// Adds two fixed point numbers together.
        /// </summary>
        /// <param name="numOne">The left-hand number to add.</param>
        /// <param name="numTwo">The right-hand number to add.</param>
        /// <returns>sum</returns>
        public static FixedPoint8 operator +(FixedPoint8 numOne, sbyte numTwo) {
            numOne.FixedValue += numTwo;
            return numOne;
        }
        
        /// <summary>
        /// Adds two fixed point numbers together.
        /// </summary>
        /// <param name="numOne">The left-hand number to add.</param>
        /// <param name="numTwo">The right-hand number to add.</param>
        /// <returns>sum</returns>
        public static sbyte operator +(sbyte numOne, FixedPoint8 numTwo) {
            return (sbyte)(numOne + numTwo.FixedValue);
        }

        /// <summary>
        /// Subtracts two fixed point numbers.
        /// The resulting number will use the settings from the first (left-hand) argument.
        /// </summary>
        /// <param name="numOne">The left-hand number to subtract from.</param>
        /// <param name="numTwo">The right-hand number to subtract.</param>
        /// <returns>difference</returns>
        public static FixedPoint8 operator -(FixedPoint8 numOne, FixedPoint8 numTwo) {
            if (numOne._decimalBits == numTwo._decimalBits) { // If the settings match, fast addition can be done.
                numOne.FixedValue -= numTwo.FixedValue;
                return numOne; // Keep settings.
            }

            // Otherwise, use the settings from the first one and keep the result.
            numOne.Value = (numOne.Value - numTwo.Value);
            return numOne; // Keep the settings from #1.
        }
        
        /// <summary>
        /// Subtracts a float from a fixed point number.
        /// The resulting number will be fixed-point and use settings from the original fixed point number.
        /// </summary>
        /// <param name="numOne">The fixed-point number to subtract from.</param>
        /// <param name="numTwo">The float number to subtract.</param>
        /// <returns>difference</returns>
        public static FixedPoint8 operator -(FixedPoint8 numOne, float numTwo) {
            numOne.Value = (numOne.Value - numTwo);
            return numOne; // Keep the settings.
        }
        
        /// <summary>
        /// Subtracts a fixed point number from a float.
        /// The resulting number will be a float.
        /// </summary>
        /// <param name="numOne">The float number to subtract from.</param>
        /// <param name="numTwo">The fixed-point number to subtract.</param>
        /// <returns>difference</returns>
        public static float operator -(float numOne, FixedPoint8 numTwo) {
            return numOne - numTwo.Value;
        }
        
        /// <summary>
        /// Subtracts a fixed point number from another one.
        /// </summary>
        /// <param name="numOne">The left-hand number to subtract from.</param>
        /// <param name="numTwo">The right-hand number to subtract.</param>
        /// <returns>difference</returns>
        public static FixedPoint8 operator -(FixedPoint8 numOne, sbyte numTwo) {
            numOne.FixedValue -= numTwo;
            return numOne;
        }
        
        /// <summary>
        /// Subtracts a fixed point number from another one.
        /// </summary>
        /// <param name="numOne">The left-hand number to subtract from.</param>
        /// <param name="numTwo">The right-hand number to subtract.</param>
        /// <returns>difference</returns>
        public static sbyte operator -(sbyte numOne, FixedPoint8 numTwo) {
            return (sbyte)(numOne - numTwo.FixedValue);
        }
        
        /// <summary>
        /// Multiplies a fixed point number with another one.
        /// Reference: https://www.allaboutcircuits.com/technical-articles/multiplication-examples-using-the-fixed-point-representation/
        /// This will not change the number of bits the number uses.
        /// </summary>
        /// <param name="numOne">The left-hand number to multiply.</param>
        /// <param name="numTwo">The right-hand number to multiply.</param>
        /// <returns>product</returns>
        public static FixedPoint8 operator *(FixedPoint8 numOne, FixedPoint8 numTwo) {
            double inversePowerOfTwo = BitsInversePowerOf2[numOne._decimalBits + numTwo._decimalBits];
            numOne.Value = (float)(numOne.FixedValue * numTwo.FixedValue * inversePowerOfTwo);
            return numOne;
        }
        
        /// <summary>
        /// Multiplies a fixed point number with a floating point number.
        /// </summary>
        /// <param name="fixedNumber">The fixed-point number to multiply.</param>
        /// <param name="floatNumber">The floating-point number to multiply.</param>
        /// <returns>product</returns>
        public static FixedPoint8 operator *(FixedPoint8 fixedNumber, float floatNumber) {
            fixedNumber.Value *= floatNumber;
            return fixedNumber;
        }
        
        /// <summary>
        /// Multiplies a fixed point number with a floating point number.
        /// </summary>
        /// <param name="floatNumber">The floating-point number to multiply.</param>
        /// <param name="fixedNumber">The fixed-point number to multiply.</param>
        /// <returns>product</returns>
        public static float operator *(float floatNumber, FixedPoint8 fixedNumber) {
            return floatNumber * fixedNumber.Value;
        }
        
        /// <summary>
        /// Multiplies a fixed point number with another one.
        /// Reference: https://www.allaboutcircuits.com/technical-articles/multiplication-examples-using-the-fixed-point-representation/
        /// Reference: https://spin.atomicobject.com/2012/03/15/simple-fixed-point-math/
        /// This will not change the number of bits the number uses.
        /// </summary>
        /// <param name="numOne">The left-hand number to multiply.</param>
        /// <param name="numTwo">The right-hand number to multiply.</param>
        /// <returns>quotient</returns>
        public static FixedPoint8 operator /(FixedPoint8 numOne, FixedPoint8 numTwo) {
            numOne.Value /= numTwo.Value;
            return numOne;
        }
        
        /// <summary>
        /// Divides a fixed point number by a floating point number.
        /// </summary>
        /// <param name="fixedNumber">The fixed-point number to divide.</param>
        /// <param name="divisor">The floating-point divisor to divide by.</param>
        /// <returns>quotient</returns>
        public static FixedPoint8 operator /(FixedPoint8 fixedNumber, float divisor) {
            fixedNumber.Value /= divisor;
            return fixedNumber;
        }
        
        /// <summary>
        /// Divides a floating-point number by a fixed-point number.
        /// </summary>
        /// <param name="floatNumber">The floating-point number to divide.</param>
        /// <param name="divisor">The fixed-point divisor to divide by.</param>
        /// <returns>quotient</returns>
        public static float operator /(float floatNumber, FixedPoint8 divisor) {
            return floatNumber / divisor.Value;
        }

        /// <summary>
        /// Test value equality between two fixed point numbers.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Are the two numeric values equal?</returns>
        public static bool operator ==(FixedPoint8 numOne, FixedPoint8 numTwo) {
            return numOne.Equals(numTwo);
        }
        
        /// <summary>
        /// Test value inequality between two fixed point numbers.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Are the two numeric values not equal?</returns>
        public static bool operator !=(FixedPoint8 numOne, FixedPoint8 numTwo) {
            return !(numOne == numTwo);
        }
        
        /// <summary>
        /// Test value equality between two fixed point numbers.
        /// </summary>
        /// <param name="fixedNum">The first fixed-point number.</param>
        /// <param name="floatNum">The second fixed-point number.</param>
        /// <returns>Are the two numeric values equal?</returns>
        public static bool operator ==(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue == NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }

        /// <summary>
        /// Test if the values of a fixed point number and a floating point number do not match.
        /// </summary>
        /// <param name="fixedNum">The first fixed-point number.</param>
        /// <param name="floatNum">The floating point number.</param>
        /// <returns>Are the two numeric values not equal?</returns>
        public static bool operator !=(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue != NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }

        /// <summary>
        /// Test value equality between two fixed point numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Are the two numeric values equal?</returns>
        public static bool operator ==(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue == numTwo;
        }

        /// <summary>
        /// Test value inequality between two fixed point numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Are the two numeric values not equal?</returns>
        public static bool operator !=(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue != numTwo;
        }

        /// <summary>
        /// Test if a fixed point number is greater than another fixed point number.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number greater than the second?</returns>
        public static bool operator >(FixedPoint8 numOne, FixedPoint8 numTwo) {
            if (numOne._decimalBits == numTwo._decimalBits)
                return numOne.FixedValue > numTwo.FixedValue;

            numOne.GetBits(out byte oneIntegerBits, out byte oneDecimalBits);
            numTwo.GetBits(out byte twoIntegerBits, out byte twoDecimalBits);
            if (oneIntegerBits != twoIntegerBits)
                return oneIntegerBits > twoIntegerBits; // If the integer part doesn't match, return.

            bool oneHasMoreDecimalBits = (oneDecimalBits > twoDecimalBits);
            int removeBitsFromOne = oneHasMoreDecimalBits ? (oneDecimalBits - twoDecimalBits) : 0;
            int removeBitsFromTwo = oneHasMoreDecimalBits ? 0 : (twoDecimalBits - oneDecimalBits);

            int finalDecimalBitsOne = (oneDecimalBits >> removeBitsFromOne);
            int finalDecimalBitsTwo = (twoDecimalBits >> removeBitsFromTwo);
            if (finalDecimalBitsOne == finalDecimalBitsTwo) // If the decimal parts match, check if the first number has any more numbers specified, which would make it larger.
                return ((oneDecimalBits & ((1 << removeBitsFromOne) - 1)) > 0);
            
            // Verify that all bits which present in both numbers are set.
            return finalDecimalBitsOne > finalDecimalBitsTwo;
        }
        
        /// <summary>
        /// Test if a fixed-point number is less than another fixed-point number.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number less than the second number?</returns>
        public static bool operator <(FixedPoint8 numOne, FixedPoint8 numTwo) {
            return !(numOne >= numTwo);
        }
        
        /// <summary>
        /// Test if a fixed point number is greater than a floating point number.
        /// </summary>
        /// <param name="fixedNum">The fixed point number.</param>
        /// <param name="floatNum">The floating point number.</param>
        /// <returns>Is the first number greater than the second number?</returns>
        public static bool operator >(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue > NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }

        /// <summary>
        /// Test if a fixed point number is less than a floating point number.
        /// </summary>
        /// <param name="fixedNum">The first fixed-point number.</param>
        /// <param name="floatNum">The floating point number.</param>
        /// <returns>Is the first number less than the second number?</returns>
        public static bool operator <(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue < NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }
        
        /// <summary>
        /// Test if a fixed point number is greater than another fixed point number.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number greater than the second number?</returns>
        public static bool operator >(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue > numTwo;
        }
        
        /// <summary>
        /// Test if a fixed point number is less than another fixed point number.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number less than the second number?</returns>
        public static bool operator <(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue < numTwo;
        }
        
        /// <summary>
        /// Test if a fixed point number is greater than or equal to another fixed point number.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number greater than or equal to the second number?</returns>
        public static bool operator >=(FixedPoint8 numOne, FixedPoint8 numTwo) {
            if (numOne._decimalBits == numTwo._decimalBits)
                return numOne.FixedValue >= numTwo.FixedValue;

            numOne.GetBits(out byte oneIntegerBits, out byte oneDecimalBits);
            numTwo.GetBits(out byte twoIntegerBits, out byte twoDecimalBits);
            if (oneIntegerBits != twoIntegerBits)
                return oneIntegerBits > twoIntegerBits; // If the integer part doesn't match, return.

            bool oneHasMoreDecimalBits = (oneDecimalBits > twoDecimalBits);
            int removeBitsFromOne = oneHasMoreDecimalBits ? (oneDecimalBits - twoDecimalBits) : 0;
            int removeBitsFromTwo = oneHasMoreDecimalBits ? 0 : (twoDecimalBits - oneDecimalBits);

            int finalDecimalBitsOne = (oneDecimalBits >> removeBitsFromOne);
            int finalDecimalBitsTwo = (twoDecimalBits >> removeBitsFromTwo);
            if (finalDecimalBitsOne == finalDecimalBitsTwo) // If the decimal parts match, check if the second number has any more numbers specified, which would make it larger, thus making the check fail.
                return ((twoDecimalBits & ((1 << removeBitsFromTwo) - 1)) == 0);
            
            // Verify that all bits which present in both numbers are set.
            return finalDecimalBitsOne >= finalDecimalBitsTwo;
        }
        
        /// <summary>
        /// Test if a fixed point number is less than another fixed point number.
        /// It does not matter whether or not the number of decimal/integer bits are the same for both the numbers.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number less than or equal to the second number?</returns>
        public static bool operator <=(FixedPoint8 numOne, FixedPoint8 numTwo) {
            return !(numOne > numTwo);
        }
        
        /// <summary>
        /// Test if a fixed point number is greater than or equal to a floating point number.
        /// </summary>
        /// <param name="fixedNum">The fixed-point number.</param>
        /// <param name="floatNum">The floating point number.</param>
        /// <returns>Is the first number greater than or equal to the second number?</returns>
        public static bool operator >=(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue >= NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }
        
        /// <summary>
        /// Test if a fixed point number is less than or equal to a floating point number.
        /// </summary>
        /// <param name="fixedNum">The first fixed-point number.</param>
        /// <param name="floatNum">The floating point number.</param>
        /// <returns>Is the first number than or equal to the second number?</returns>
        public static bool operator <=(FixedPoint8 fixedNum, float floatNum) {
            return fixedNum.FixedValue <= NumberUtils.ConvertFloatToFixedPointSByte(floatNum, fixedNum.NumberOfDecimalBits);
        }
        
        /// <summary>
        /// Test if a fixed point number is greater than or equal to another fixed point number.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number greater than or equal to the second number?</returns>
        public static bool operator >=(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue >= numTwo;
        }
        
        /// <summary>
        /// Test if a fixed point number is less than or equal to another fixed point number.
        /// </summary>
        /// <param name="numOne">The first fixed-point number.</param>
        /// <param name="numTwo">The second fixed-point number.</param>
        /// <returns>Is the first number less than or equal to the second number?</returns>
        public static bool operator <=(FixedPoint8 numOne, sbyte numTwo) {
            return numOne.FixedValue <= numTwo;
        }
        
        /// <summary>
        /// Shifts a fixed point number to the left.
        /// </summary>
        /// <param name="number">The number to shift.</param>
        /// <param name="shiftAmount">The number of bits to shift the number by.</param>
        /// <returns>Shifted number</returns>
        public static FixedPoint8 operator <<(FixedPoint8 number, int shiftAmount) {
            if (shiftAmount < 0 || shiftAmount > BitCount)
                throw new ArgumentOutOfRangeException(nameof(shiftAmount), $"A {nameof(FixedPoint8)} cannot be shifted left {shiftAmount} bits. (Must be between 0 and {BitCount}.)");

            if (shiftAmount > 0)
                number.FixedValue = (sbyte)((number.FixedValue << shiftAmount) & 0xFF);
            return number;
        }
        
        /// <summary>
        /// Shifts a fixed point number to the right.
        /// </summary>
        /// <param name="number">The number to shift.</param>
        /// <param name="shiftAmount">The number of bits to shift the number by.</param>
        /// <returns>Shifted number</returns>
        public static FixedPoint8 operator >>(FixedPoint8 number, int shiftAmount) {
            if (shiftAmount < 0 || shiftAmount > BitCount)
                throw new ArgumentOutOfRangeException(nameof(shiftAmount), $"A {nameof(FixedPoint8)} cannot be shifted right {shiftAmount} bits. (Must be between 0 and {BitCount}.)");
            if (shiftAmount > 0) 
                number.FixedValue = (sbyte)((number.FixedValue >> shiftAmount) & 0xFF);
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise and operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator &(FixedPoint8 number, FixedPoint8 bitMask) {
            number.FixedValue &= bitMask.FixedValue;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise and operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator &(FixedPoint8 number, sbyte bitMask) {
            number.FixedValue &= bitMask;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise and operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator &(FixedPoint8 number, int bitMask) {
            number.FixedValue = (sbyte)(number.FixedValue & bitMask);
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise OR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator |(FixedPoint8 number, FixedPoint8 bitMask) {
            number.FixedValue |= bitMask.FixedValue;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise OR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator |(FixedPoint8 number, sbyte bitMask) {
            number.FixedValue |= bitMask;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise OR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator |(FixedPoint8 number, int bitMask) {
            if ((bitMask & 0xFF) != bitMask)
                throw new ArgumentOutOfRangeException(nameof(bitMask), $"Mask value {bitMask} contains bits which cannot be set.");
            
            number.FixedValue = unchecked((sbyte)(((byte)number.FixedValue) | bitMask));
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise XOR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator ^(FixedPoint8 number, FixedPoint8 bitMask) {
            number.FixedValue ^= bitMask.FixedValue;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise XOR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator ^(FixedPoint8 number, sbyte bitMask) {
            number.FixedValue ^= bitMask;
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise XOR operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <param name="bitMask">The bit mask to apply.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator ^(FixedPoint8 number, int bitMask) {
            if ((bitMask & 0xFF) != bitMask)
                throw new ArgumentOutOfRangeException(nameof(bitMask), $"Mask value {bitMask} contains bits which cannot be set.");
            
            number.FixedValue = (sbyte)(number.FixedValue ^ bitMask);
            return number;
        }
        
        /// <summary>
        /// Performs a bitwise NOT operation.
        /// </summary>
        /// <param name="number">The number to apply the mask to.</param>
        /// <returns>result</returns>
        public static FixedPoint8 operator ~(FixedPoint8 number) {
            number.FixedValue = (sbyte)~number.FixedValue;
            return number;
        }

        /// <summary>
        /// Performs a modulo operation on a fixed point number.
        /// Reference: https://blog.mbedded.ninja/programming/general/fixed-point-mathematics/
        /// </summary>
        /// <param name="number">The number to get the modulo of.</param>
        /// <param name="divisor">The divisor to use to compute the modulo.</param>
        /// <returns>modulo</returns>
        public static FixedPoint8 operator %(FixedPoint8 number, FixedPoint8 divisor) {
            if (number._decimalBits == divisor._decimalBits) {
                number.FixedValue %= divisor.FixedValue;
                return number;
            }

            int fixedValue = (number.FixedValue << number._decimalBits);
            int divisorFixedValue = (divisor.FixedValue << divisor._decimalBits);
            number.FixedValue = unchecked((sbyte)((fixedValue % divisorFixedValue) >> number._decimalBits));
            return number;
        }
        
        /// <summary>
        /// Negates the fixed point value.
        /// </summary>
        /// <param name="number">The number to negate.</param>
        /// <returns>negatedNumber</returns>
        public static FixedPoint8 operator -(FixedPoint8 number) {
            number.FixedValue = (sbyte)-number.FixedValue;
            return number;
        }
        
        /// <summary>
        /// Performs a modulo operation on a fixed point number.
        /// Reference: https://blog.mbedded.ninja/programming/general/fixed-point-mathematics/
        /// </summary>
        /// <param name="number">The number to get the modulo of.</param>
        /// <param name="divisor">The divisor to use to compute the modulo.</param>
        /// <returns>modulo</returns>
        public static FixedPoint8 operator %(FixedPoint8 number, float divisor) {
            number.Value %= divisor;
            return number;
        }
        
        /// <summary>
        /// Performs a modulo operation on a fixed point number.
        /// Reference: https://blog.mbedded.ninja/programming/general/fixed-point-mathematics/
        /// </summary>
        /// <param name="number">The number to get the modulo of.</param>
        /// <param name="divisor">The divisor to use to compute the modulo.</param>
        /// <returns>modulo</returns>
        public static FixedPoint8 operator %(FixedPoint8 number, sbyte divisor) {
            number.FixedValue %= divisor;
            return number;
        }
    }
}