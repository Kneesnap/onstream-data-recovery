using ModToolFramework.Utils.Extensions;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ModToolFramework.Utils {
    public enum NumberParseFailureReason {
        None,
        BadFormat,
        Overflow,
        UnexpectedCharacter
    }

    /// <summary>
    /// Contains various number-related utilities.
    /// </summary>
    public static class NumberUtils {
        /// <summary>
        /// Tests that the half floating point value is not Nan and not infinity.
        /// </summary>
        /// <param name="halfVal">The float number to test.</param>
        /// <returns>isValidFloatingPoint</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(Half halfVal) {
            return !Half.IsNaN(halfVal) && !Half.IsInfinity(halfVal);
        }
        
        /// <summary>
        /// Tests that the floating point value is not Nan and not infinity.
        /// </summary>
        /// <param name="floatVal">The float number to test.</param>
        /// <returns>isValidFloatingPoint</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(float floatVal) {
            return !float.IsNaN(floatVal) && !float.IsInfinity(floatVal);
        }
        
        /// <summary>
        /// Tests that the double floating point value is not Nan and not infinity.
        /// </summary>
        /// <param name="doubleVal">The double number to test.</param>
        /// <returns>isValidFloatingPoint</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRealNumber(double doubleVal) {
            return !double.IsNaN(doubleVal) && !double.IsInfinity(doubleVal);
        }
        
        /// <summary>
        /// Tests if the given input string is a valid sbyte.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidSByte</returns>
        public static bool IsValidSByte(string str) {
            return TryParseSByte(str, out sbyte _);
        }

        /// <summary>
        /// Parses an sbyte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting sbyte value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid sbyte.</exception>
        public static sbyte ParseSByte(string inputStr) {
            bool success = TryParseSByte(inputStr, out sbyte resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses an sbyte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting sbyte value.</param>
        /// <returns>Whether or not the integer was successfully parsed.</returns>
        public static bool TryParseSByte(string inputStr, out sbyte resultValue) {
            return TryParseSByte(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses an sbyte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting sbyte value.</param>
        /// <param name="failReason">The reason the sbyte couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the sbyte was successfully parsed.</returns>
        public static bool TryParseSByte(string inputStr, out sbyte resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            int i = isNegative ? 1 : 0;
            if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'x') {
                i += 2;
                if (inputStr.Length == i) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    sbyte hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (sbyte)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = (sbyte)(tempChar - 'A' + 10);
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = (sbyte)(tempChar - 'a' + 10);
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF0) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'b') {
                i += 2;
                if (inputStr.Length == i) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag7) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (sbyte)(bitTrue ? 1 : 0);
                }
            } else {
                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    sbyte digit = (sbyte)(tempChar - '0');
                    const sbyte maxValue = SByte.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (SByte.MaxValue - (maxValue * 10) + (isNegative ? 1 : 0)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            if (isNegative)
                resultValue *= -1;

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid byte.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidByte</returns>
        public static bool IsValidByte(string str) {
            return TryParseByte(str, out byte _);
        }

        /// <summary>
        /// Parses a byte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting byte value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid byte.</exception>
        public static byte ParseByte(string inputStr) {
            bool success = TryParseByte(inputStr, out byte resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a byte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting byte value.</param>
        /// <returns>Whether or not the byte was successfully parsed.</returns>
        public static bool TryParseByte(string inputStr, out byte resultValue) {
            return TryParseByte(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a byte string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting byte value.</param>
        /// <param name="failReason">The reason the byte couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the byte was successfully parsed.</returns>
        public static bool TryParseByte(string inputStr, out byte resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'x') {
                if (inputStr.Length == 2) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    byte hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (byte)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = (byte)(tempChar - 'A' + 10);
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = (byte)(tempChar - 'a' + 10);
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF0) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'b') {
                if (inputStr.Length == 2) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag7) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (byte)(bitTrue ? 1 : 0);
                }
            } else {
                for (int i = 0; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    byte digit = (byte)(tempChar - '0');
                    const byte maxValue = Byte.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (Byte.MaxValue - (maxValue * 10)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid short.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidShort</returns>
        public static bool IsValidShort(string str) {
            return TryParseShort(str, out short _);
        }

        /// <summary>
        /// Parses an short string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting short value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid short.</exception>
        public static short ParseShort(string inputStr) {
            bool success = TryParseShort(inputStr, out short resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses an short string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting short value.</param>
        /// <returns>Whether or not the short was successfully parsed.</returns>
        public static bool TryParseShort(string inputStr, out short resultValue) {
            return TryParseShort(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses an short string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting short value.</param>
        /// <param name="failReason">The reason the short couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the short was successfully parsed.</returns>
        public static bool TryParseShort(string inputStr, out short resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            int i = isNegative ? 1 : 0;
            if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'x') {
                i += 2;
                if (inputStr.Length == i) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    short hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (short)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = (short)(tempChar - 'A' + 10);
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = (short)(tempChar - 'a' + 10);
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF000) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'b') {
                i += 2;
                if (inputStr.Length == i) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag15) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (short)(bitTrue ? 1 : 0);
                }
            } else {
                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    short digit = (short)(tempChar - '0');
                    const short maxValue = Int16.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (Int16.MaxValue - (maxValue * 10) + (isNegative ? 1 : 0)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            if (isNegative)
                resultValue *= -1;

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid ushort.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidUShort</returns>
        public static bool IsValidUShort(string str) {
            return TryParseUShort(str, out ushort _);
        }

        /// <summary>
        /// Parses a ushort string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting ushort value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid ushort.</exception>
        public static ushort ParseUShort(string inputStr) {
            bool success = TryParseUShort(inputStr, out ushort resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a ushort string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting ushort value.</param>
        /// <returns>Whether or not the ushort was successfully parsed.</returns>
        public static bool TryParseUShort(string inputStr, out ushort resultValue) {
            return TryParseUShort(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a ushort string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting ushort value.</param>
        /// <param name="failReason">The reason the ushort couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the ushort was successfully parsed.</returns>
        public static bool TryParseUShort(string inputStr, out ushort resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'x') {
                if (inputStr.Length == 2) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    ushort hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (ushort)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = (ushort)(tempChar - 'A' + 10);
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = (ushort)(tempChar - 'a' + 10);
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF000) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'b') {
                if (inputStr.Length == 2) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag15) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (ushort)(bitTrue ? 1 : 0);
                }
            } else {
                for (int i = 0; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    ushort digit = (ushort)(tempChar - '0');
                    const ushort maxValue = UInt16.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (UInt16.MaxValue - (maxValue * 10)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid int32.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidInteger</returns>
        public static bool IsValidInteger(string str) {
            return TryParseInteger(str, out int _);
        }

        /// <summary>
        /// Parses an int32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting integer value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid integer.</exception>
        public static int ParseInteger(string inputStr) {
            bool success = TryParseInteger(inputStr, out int resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses an int32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting integer value.</param>
        /// <returns>Whether or not the integer was successfully parsed.</returns>
        public static bool TryParseInteger(string inputStr, out int resultValue) {
            return TryParseInteger(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses an int32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting integer value.</param>
        /// <param name="failReason">The reason the integer couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the integer was successfully parsed.</returns>
        public static bool TryParseInteger(string inputStr, out int resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            int i = isNegative ? 1 : 0;
            if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'x') {
                i += 2;
                if (inputStr.Length == i) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    int hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = 10 + (tempChar - 'A');
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = 10 + (tempChar - 'a');
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF0000000) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'b') {
                i += 2;
                if (inputStr.Length == i) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag31) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (bitTrue ? 1 : 0);
                }
            } else {
                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    int digit = (tempChar - '0');
                    const int maxValue = Int32.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (Int32.MaxValue - (maxValue * 10) + (isNegative ? 1 : 0)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            if (isNegative)
                resultValue *= -1;

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid uint32.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidUnsignedInteger</returns>
        public static bool IsValidUnsignedInteger(string str) {
            return TryParseUnsignedInteger(str, out uint _);
        }

        /// <summary>
        /// Parses a uint32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting integer value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid unsigned integer.</exception>
        public static uint ParseUnsignedInteger(string inputStr) {
            bool success = TryParseUnsignedInteger(inputStr, out uint resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a uint32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting integer value.</param>
        /// <returns>Whether or not the unsigned integer was successfully parsed.</returns>
        public static bool TryParseUnsignedInteger(string inputStr, out uint resultValue) {
            return TryParseUnsignedInteger(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a uint32 string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting integer value.</param>
        /// <param name="failReason">The reason the integer couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the unsigned integer was successfully parsed.</returns>
        public static bool TryParseUnsignedInteger(string inputStr, out uint resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'x') {
                if (inputStr.Length == 2) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    uint hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (uint)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = 10 + (uint)(tempChar - 'A');
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = 10 + (uint)(tempChar - 'a');
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF0000000) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'b') {
                if (inputStr.Length == 2) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag31) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (bitTrue ? 1U : 0U);
                }
            } else {
                for (int i = 0; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    uint digit = (uint)(tempChar - '0');
                    const uint maxValue = UInt32.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (UInt32.MaxValue - (maxValue * 10)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid long.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidLong</returns>
        public static bool IsValidLong(string str) {
            return TryParseLong(str, out long _);
        }

        /// <summary>
        /// Parses a long string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting long value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid long.</exception>
        public static long ParseLong(string inputStr) {
            bool success = TryParseLong(inputStr, out long resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a long string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting long value.</param>
        /// <returns>Whether or not the long was successfully parsed.</returns>
        public static bool TryParseLong(string inputStr, out long resultValue) {
            return TryParseLong(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a long string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed. Negative numbers are supported.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting long value.</param>
        /// <param name="failReason">The reason the long couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the long was successfully parsed.</returns>
        public static bool TryParseLong(string inputStr, out long resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            int i = isNegative ? 1 : 0;
            if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'x') {
                i += 2;
                if (inputStr.Length == i) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    long hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = 10 + (tempChar - 'A');
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = 10 + (tempChar - 'a');
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & unchecked((long)0xF000000000000000L)) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > i + 1 && inputStr[i] == '0' && inputStr[i + 1] == 'b') {
                i += 2;
                if (inputStr.Length == i) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & DataConstants.BitFlag63) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (bitTrue ? 1U : 0U);
                }
            } else {
                for (; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    int digit = (tempChar - '0');
                    const long maxValue = Int64.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (Int64.MaxValue - (maxValue * 10) + (isNegative ? 1 : 0)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            if (isNegative)
                resultValue *= -1;

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid long.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidULong</returns>
        public static bool IsValidULong(string str) {
            return TryParseULong(str, out ulong _);
        }

        /// <summary>
        /// Parses a ulong string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting ulong value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid ulong.</exception>
        public static ulong ParseULong(string inputStr) {
            bool success = TryParseULong(inputStr, out ulong resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a ulong string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting ulong value.</param>
        /// <returns>Whether or not the ulong was successfully parsed.</returns>
        public static bool TryParseULong(string inputStr, out ulong resultValue) {
            return TryParseULong(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a ulong string in one of its various formats.
        /// Hex, Binary, and Base 10 numbers are allowed.
        /// Hex numbers must be prefixed with '0x', binary numbers must be prefixed with '0b'.
        /// Type markers like 'L' or 'U' are not supported.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting ulong value.</param>
        /// <param name="failReason">The reason the ulong couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the ulong was successfully parsed.</returns>
        public static bool TryParseULong(string inputStr, out ulong resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'x') {
                if (inputStr.Length == 2) { // "0x"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    ulong hexValue;
                    if (tempChar >= '0' && tempChar <= '9') {
                        hexValue = (uint)(tempChar - '0');
                    } else if (tempChar >= 'A' && tempChar <= 'F') {
                        hexValue = 10 + (uint)(tempChar - 'A');
                    } else if (tempChar >= 'a' && tempChar <= 'f') {
                        hexValue = 10 + (uint)(tempChar - 'a');
                    } else {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & 0xF000000000000000) != 0) { // If these bits are set, wrap around will occur.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 4; // Base 16, Multiply by 16.
                    resultValue |= hexValue;
                }
            } else if (inputStr.Length > 1 && inputStr[0] == '0' && inputStr[1] == 'b') {
                if (inputStr.Length == 2) { // "0b"
                    failReason = NumberParseFailureReason.BadFormat;
                    return false;
                }

                for (int i = 2; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    bool bitTrue = (tempChar == '1');
                    if (!bitTrue && tempChar != '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    if ((resultValue & unchecked((ulong)DataConstants.BitFlag63)) != 0) { // About to overflow.
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue <<= 1; // Base 2, Multiply by 2.
                    resultValue |= (bitTrue ? 1U : 0U);
                }
            } else {
                for (int i = 0; i < inputStr.Length; i++) {
                    char tempChar = inputStr[i];
                    if (tempChar > '9' || tempChar < '0') {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    uint digit = (uint)(tempChar - '0');
                    const ulong maxValue = UInt64.MaxValue / 10;
                    if (resultValue > maxValue || (resultValue == maxValue && digit > (UInt64.MaxValue - (maxValue * 10)))) {
                        failReason = NumberParseFailureReason.Overflow;
                        return false;
                    }

                    resultValue *= 10;
                    resultValue += digit;
                }
            }

            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Tests if a given string is a valid hex number.
        /// Does not allow a prefix like 'L', or 'U', at the end.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>IsValidHexNumber</returns>
        public static bool IsHexNumber(string str) {
            if (str == null)
                return false;

            bool isNegative = str.StartsWith("-", StringComparison.InvariantCulture);
            int baseIndex = isNegative ? 1 : 0;
            if (str.Length <= (2 + baseIndex) || str[baseIndex] != '0' || str[baseIndex + 1] != 'x')
                return false;

            for (int i = 2 + baseIndex; i < str.Length; i++)
                if (!GeneralUtils.IsHexadecimal(str[i]))
                    return false;

            return true;
        }

        /// <summary>
        /// Tests if the given input string is a valid decimal or integer number.
        /// Only supports raw numbers. Doesn't support hex, doesn't support others.
        /// </summary>
        /// <param name="input">The input to test.</param>
        /// <returns>isValidNumber</returns>
        public static bool IsValidNumber(string input) {
            if (String.IsNullOrEmpty(input))
                return false;

            bool hasDecimal = false;
            bool hasAnyDigit = false;
            for (int i = 0; i < input.Length; i++) {
                char test = input[i];
                if (test == '-' && i == 0)
                    continue; // Allow negative indicator.

                if (test == '.') {
                    if (!hasDecimal) {
                        hasDecimal = true;
                        hasAnyDigit = false; // Require further digit.
                        continue;
                    } else {
                        return false; // Multiple decimal = invalid number.
                    }
                }

                if (!GeneralUtils.IsDigit(test)) {
                    return false; // Character isn't a digit, so it can't be a number.
                } else {
                    hasAnyDigit = true;
                }
            }

            return hasAnyDigit;
        }
        
        /// <summary>
        /// Tests if the given input string is a valid float.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidFloat</returns>
        public static bool IsValidHalf(string str) {
            return TryParseHalf(str, out _);
        }

        /// <summary>
        /// Parses a half-float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting half-float value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid half-float.</exception>
        public static Half ParseHalf(string inputStr) {
            bool success = TryParseHalf(inputStr, out Half resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a half-float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting half-float value.</param>
        /// <returns>Whether or not the half-float was successfully parsed.</returns>
        public static bool TryParseHalf(string inputStr, out Half resultValue) {
            return TryParseHalf(inputStr, out resultValue, out _);
        }

        /// <summary>
        /// Parses a half-float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting half-float value.</param>
        /// <param name="failReason">The reason the half-float couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the half-float was successfully parsed.</returns>
        public static bool TryParseHalf(string inputStr, out Half resultValue, out NumberParseFailureReason failReason) {
            bool result = TryParseFloat(inputStr, out float resultFloatValue, out failReason);
            resultValue = (Half)resultFloatValue;
            return result;
        }

        /// <summary>
        /// Tests if the given input string is a valid float.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidFloat</returns>
        public static bool IsValidFloat(string str) {
            return TryParseFloat(str, out _);
        }

        /// <summary>
        /// Parses a float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting float value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid float.</exception>
        public static float ParseFloat(string inputStr) {
            bool success = TryParseFloat(inputStr, out float resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting float value.</param>
        /// <returns>Whether or not the float was successfully parsed.</returns>
        public static bool TryParseFloat(string inputStr, out float resultValue) {
            return TryParseFloat(inputStr, out resultValue, out _);
        }

        /// <summary>
        /// Parses a float string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting float value.</param>
        /// <param name="failReason">The reason the float couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the float was successfully parsed.</returns>
        public static bool TryParseFloat(string inputStr, out float resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool seenDecimal = false;
            for (int i = isNegative ? 1 : 0; i < inputStr.Length; i++) {
                char tempChar = inputStr[i];

                if (tempChar == '.') {
                    if (seenDecimal) {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }
                    
                    seenDecimal = true;
                } else if (tempChar > '9' || tempChar < '0') {
                    failReason = NumberParseFailureReason.UnexpectedCharacter;
                    return false;
                }
            }

            resultValue = (float)Convert.ToDouble(inputStr, CultureInfo.InvariantCulture);
            failReason = NumberParseFailureReason.None;
            return true;
        }
        
        /// <summary>
        /// Tests if the given input string is a valid double.
        /// </summary>
        /// <param name="str">The string to test.</param>
        /// <returns>isValidDouble</returns>
        public static bool IsValidDouble(string str) {
            return TryParseDouble(str, out double _);
        }

        /// <summary>
        /// Parses a double string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'D' or 'd' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <returns>The resulting double value.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not a valid double.</exception>
        public static double ParseDouble(string inputStr) {
            bool success = TryParseDouble(inputStr, out double resultValue, out NumberParseFailureReason failReason);
            if (!success)
                throw new FormatException($"The number string '{inputStr}' could not be parsed. (Reason: {failReason.GetName()})");
            return resultValue;
        }

        /// <summary>
        /// Parses a double string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'D' or 'd' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting double value.</param>
        /// <returns>Whether or not the double was successfully parsed.</returns>
        public static bool TryParseDouble(string inputStr, out double resultValue) {
            return TryParseDouble(inputStr, out resultValue, out NumberParseFailureReason _);
        }

        /// <summary>
        /// Parses a double string.
        /// Negative numbers are allowed. Hex and binary numbers are not.
        /// Type markers like 'F' or 'f' are not supported. Neither is scientific notation 'e'.
        /// </summary>
        /// <param name="inputStr">The string to parse as a number.</param>
        /// <param name="resultValue">The resulting double value.</param>
        /// <param name="failReason">The reason the double couldn't be parsed. (Or None, if it could be parsed.)</param>
        /// <returns>Whether or not the double was successfully parsed.</returns>
        public static bool TryParseDouble(string inputStr, out double resultValue, out NumberParseFailureReason failReason) {
            resultValue = 0;

            if (string.IsNullOrEmpty(inputStr)) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool isNegative = inputStr[0] == '-';
            if (isNegative && inputStr.Length == 1) {
                failReason = NumberParseFailureReason.BadFormat;
                return false;
            }

            bool seenDecimal = false;
            for (int i = isNegative ? 1 : 0; i < inputStr.Length; i++) {
                char tempChar = inputStr[i];

                if (tempChar == '.') {
                    if (seenDecimal) {
                        failReason = NumberParseFailureReason.UnexpectedCharacter;
                        return false;
                    }

                    seenDecimal = true;
                } else if (tempChar > '9' || tempChar < '0') {
                    failReason = NumberParseFailureReason.UnexpectedCharacter;
                    return false;
                }
            }

            resultValue = Convert.ToDouble(inputStr, CultureInfo.InvariantCulture);
            failReason = NumberParseFailureReason.None;
            return true;
        }

        /// <summary>
        /// Performs a square-root on a fixed-point number.
        /// NOTE: This probably should go somewhere else later.
        /// </summary>
        /// <param name="i">The number to square root.</param>
        /// <returns>result</returns>
        public static int FixedPointSqrt(int i) {
            return (int)Math.Sqrt(i); // Lucky for us, this is actually somehow the correct response, or at least it seems to be. I'm making this a method so in-case this ever needs to be changed in the future it can be.
        }

        //TODO: Wiki needs documentation on how fixed point / floating point works. (Include diagrams + explanations of how it works, as well as overview of what each console uses.)

        // Confused about what these functions do?
        // Don't know the difference between fixed-point and floating point numbers?
        // Check out the section on fixed point numbers on the repository wiki.
        // General Criteria for when fixed point math was used:
        // - Fixed-point calculations were done before hardware to perform floating point operations very fast was available.
        // - Most PlayStation 1 games (and basically any game that used 3D graphics before this) likely use fixed point math.
        // - Certain games on Windows which came out during this timeframe (approximately pre-2001) also use fixed point math.

        /// <summary>
        /// Converts a fixed point byte to a floating point value.
        /// The highest bits are the decimal portion.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static float ConvertFixedPointByteToFloat(byte value, int decimalBits) {
            if (decimalBits < 0 || decimalBits > DataConstants.ByteBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ByteBitCount}].");
            return ((float)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point byte, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static byte ConvertFloatToFixedPointByte(float value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as a fixed-point number.");
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as an unsigned fixed-point number.");
            if (decimalBits < 0 || decimalBits > DataConstants.ByteBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ByteBitCount}].");
            return (byte)MathF.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point sbyte to a floating point value.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static float ConvertFixedPointSByteToFloat(sbyte value, int decimalBits) {
            if (decimalBits < 0 || decimalBits >= DataConstants.ByteBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ByteBitCount}).");
            return ((float)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point sbyte, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static sbyte ConvertFloatToFixedPointSByte(float value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as a fixed-point number.");

            if (decimalBits < 0 || decimalBits >= DataConstants.ByteBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ByteBitCount}).");
            return (sbyte)MathF.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point short to a floating point value.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static float ConvertFixedPointShortToFloat(short value, int decimalBits) {
            if (decimalBits < 0 || decimalBits >= DataConstants.ShortBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ShortBitCount}).");
            return ((float)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point short, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static short ConvertFloatToFixedPointShort(float value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as a fixed-point number.");
            
            if (decimalBits < 0 || decimalBits >= DataConstants.ShortBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ShortBitCount}).");
            return (short)MathF.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point ushort to a floating point value.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static float ConvertFixedPointUShortToFloat(ushort value, int decimalBits) {
            if (decimalBits < 0 || decimalBits > DataConstants.ShortBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ShortBitCount}].");
            return ((float)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point ushort, rounding if necessary.
        /// The decimal portion covers the least significant bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static ushort ConvertFloatToFixedPointUShort(float value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as a fixed-point number.");
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"Float {value} cannot be represented as an unsigned fixed-point number.");

            if (decimalBits < 0 || decimalBits > DataConstants.ShortBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.ShortBitCount}].");
            return (ushort)MathF.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point int to a floating point double value.
        /// The decimal portion covers the least significant bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static double ConvertFixedPointIntToDouble(int value, int decimalBits) {
            if (decimalBits < 0 || decimalBits >= DataConstants.IntegerBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.IntegerBitCount}).");
            return ((double)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point int, rounding if necessary.
        /// The decimal portion covers the least significant bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static int ConvertDoubleToFixedPointInt(double value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as a fixed-point number.");
            if (decimalBits < 0 || decimalBits >= DataConstants.IntegerBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.IntegerBitCount}).");
            return (int)Math.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point uint to a floating point double value.
        /// The decimal portion covers the least significant bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static double ConvertFixedPointUIntToDouble(uint value, int decimalBits) {
            if (decimalBits < 0 || decimalBits > DataConstants.IntegerBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.IntegerBitCount}].");
            return ((double)value / (1 << decimalBits));
        }

        /// <summary>
        /// Converts a floating point value to a fixed point uint, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static uint ConvertDoubleToFixedPointUInt(double value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as a fixed-point number.");
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as an unsigned fixed-point number.");
            if (decimalBits < 0 || decimalBits > DataConstants.IntegerBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.IntegerBitCount}].");
            return (uint)Math.Round(value * (1 << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point long to a floating point value.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static double ConvertFixedPointLongToDouble(long value, int decimalBits) {
            if (decimalBits < 0 || decimalBits >= DataConstants.LongBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.LongBitCount}).");
            return ((double)value / (1L << decimalBits));
        }

        /// <summary>
        /// Converts a floating point double to a fixed point long, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static long ConvertDoubleToFixedPointLong(double value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as a fixed-point number.");
            if (decimalBits < 0 || decimalBits >= DataConstants.LongBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.LongBitCount}).");
            return (long)Math.Round(value * (1L << decimalBits));
        }

        /// <summary>
        /// Converts a fixed point ulong to a floating point double value.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert to fixed point.</param>
        /// <param name="decimalBits">The number of bits to be used for the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static double ConvertFixedPointULongToDouble(ulong value, int decimalBits) {
            if (decimalBits < 0 || decimalBits > DataConstants.LongBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.LongBitCount}].");
            return ((double)value / (1L << decimalBits));
        }

        /// <summary>
        /// Converts a floating point double to a fixed point ulong, rounding if necessary.
        /// The decimal portion covers the highest bits.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <param name="decimalBits">The number of bits to use to encode the decimal data.</param>
        /// <returns>convertedNumber</returns>
        public static ulong ConvertDoubleToFixedPointULong(double value, int decimalBits) {
            if (!IsRealNumber(value))
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as a fixed-point number.");
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"Double {value} cannot be represented as an unsigned fixed-point number.");
            if (decimalBits < 0 || decimalBits > DataConstants.LongBitCount)
                throw new ArgumentOutOfRangeException($"Invalid bit value '{decimalBits}'. Must be in the range [0,{DataConstants.LongBitCount}].");
            return (ulong)Math.Round(value * (1L << decimalBits));
        }
    }
}