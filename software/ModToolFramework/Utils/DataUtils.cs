using ModToolFramework.Utils.Data;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace ModToolFramework.Utils
{
    /// <summary>
    /// A class which contains utilities for working with raw data.
    /// </summary>
    public static class DataUtils
    {
        /// <summary>
        /// Switch the endian of a byte[]. If it's little endian, it will convert to big endian, and vice-versa.
        /// </summary>
        /// <param name="buffer">The byte[] to work on.</param>
        /// <returns>inputBufferWithNewEndian</returns>
        public static byte[] SwitchEndian(byte[] buffer) {
            return GeneralUtils.ReverseArray(buffer);
        }

        private static void VerifyReadOk<T>(T[] buffer, int startIndex, int requiredBytes) {
            if (startIndex < 0)
                throw new IndexOutOfRangeException("startIndex cannot be negative.");
            if (buffer.Length - startIndex < requiredBytes) // Doesn't have all of the data required.
                throw new InvalidDataException("buffer must have at least " + requiredBytes + " bytes from the startIndex. (Has: " + (buffer.Length - startIndex) + ")");
        }

        /// <summary>
        /// Converts a short to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertShortToByteArray(short value) {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a short.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static short ConvertByteArrayToShort(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 2);
            return (short)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8);
        }

        /// <summary>
        /// Converts a ushort to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertUShortToByteArray(ushort value) {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a ushort.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static ushort ConvertByteArrayToUShort(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 2);
            return (ushort)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8);
        }

        /// <summary>
        /// Converts an int to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertIntToByteArray(int value) {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into an int.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static int ConvertByteArrayToInt(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 4);
            return (buffer[startIndex + 0] | buffer[startIndex + 1] << 8 | buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
        }

        /// <summary>
        /// Converts a uint to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertUIntToByteArray(uint value) {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a uint.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static uint ConvertByteArrayToUInt(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 4);
            return (uint)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8 | buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
        }

        /// <summary>
        /// Converts a long to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertLongToByteArray(long value) {
            byte[] buffer = new byte[8];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a long.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static long ConvertByteArrayToLong(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 8);
            uint lo = (uint)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8 |
                             buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
            uint hi = (uint)(buffer[startIndex + 4] | buffer[startIndex + 5] << 8 |
                             buffer[startIndex + 6] << 16 | buffer[startIndex + 7] << 24);
            return (long)(hi) << 32 | lo;
        }

        /// <summary>
        /// Converts a ulong to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertULongToByteArray(ulong value) {
            byte[] buffer = new byte[8];
            buffer[0] = (byte)value;
            buffer[1] = (byte)(value >> 8);
            buffer[2] = (byte)(value >> 16);
            buffer[3] = (byte)(value >> 24);
            buffer[4] = (byte)(value >> 32);
            buffer[5] = (byte)(value >> 40);
            buffer[6] = (byte)(value >> 48);
            buffer[7] = (byte)(value >> 56);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a ulong.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static ulong ConvertByteArrayToULong(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 8);

            uint lo = (uint)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8 |
                             buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
            uint hi = (uint)(buffer[startIndex + 4] | buffer[startIndex + 5] << 8 |
                             buffer[startIndex + 6] << 16 | buffer[startIndex + 7] << 24);
            return ((ulong)hi) << 32 | lo;
        }
        
        /// <summary>
        /// Converts a half to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertHalfToByteArray(Half value) {
            byte[] buffer = new byte[2];
            Unsafe.As<byte, Half>(ref buffer[0]) = value;
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a half.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        [SecuritySafeCritical]
        public unsafe static Half ConvertByteArrayToHalf(byte[] buffer, int startIndex = 0) {
            ushort shortValue = ConvertByteArrayToUShort(buffer, startIndex);
            return *(Half*)&shortValue;
        }

        /// <summary>
        /// Converts a float to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        [SecuritySafeCritical]
        public unsafe static byte[] ConvertFloatToByteArray(float value) {
            uint tmpValue = *(uint*)&value; // Yo this is awesome, I didn't know C# could do this.
            byte[] buffer = new byte[4];
            buffer[0] = (byte)tmpValue;
            buffer[1] = (byte)(tmpValue >> 8);
            buffer[2] = (byte)(tmpValue >> 16);
            buffer[3] = (byte)(tmpValue >> 24);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a float.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        [SecuritySafeCritical]
        public unsafe static float ConvertByteArrayToFloat(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 4);

            uint tmpBuffer = (uint)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8 | buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
            return *((float*)&tmpBuffer);
        }

        /// <summary>
        /// Converts a double to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        [SecuritySafeCritical]
        public unsafe static byte[] ConvertDoubleToByteArray(double value) {
            ulong tmpValue = *(ulong*)&value; // Yo this is awesome, I didn't know C# could do this.
            byte[] buffer = new byte[8];
            buffer[0] = (byte)tmpValue;
            buffer[1] = (byte)(tmpValue >> 8);
            buffer[2] = (byte)(tmpValue >> 16);
            buffer[3] = (byte)(tmpValue >> 24);
            buffer[4] = (byte)(tmpValue >> 32);
            buffer[5] = (byte)(tmpValue >> 40);
            buffer[6] = (byte)(tmpValue >> 48);
            buffer[7] = (byte)(tmpValue >> 56);
            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a double.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        [SecuritySafeCritical]
        public unsafe static double ConvertByteArrayToDouble(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 8);

            uint lo = (uint)(buffer[startIndex + 0] | buffer[startIndex + 1] << 8 |
                             buffer[startIndex + 2] << 16 | buffer[startIndex + 3] << 24);
            uint hi = (uint)(buffer[startIndex + 4] | buffer[startIndex + 5] << 8 |
                             buffer[startIndex + 6] << 16 | buffer[startIndex + 7] << 24);

            ulong tmpBuffer = ((ulong)hi) << 32 | lo;
            return *((double*)&tmpBuffer);
        }

        /// <summary>
        /// Converts a decimal to a little-endian byte-array.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>byteArray</returns>
        public static byte[] ConvertDecimalToByteArray(decimal value) {
            int[] bits = decimal.GetBits(value);
            byte[] buffer = new byte[16];

            int i = 0;
            int j = 0;
            while (i < 16) {
                if (bits.Length > j) {
                    byte[] bytes = BitConverter.GetBytes(bits[j++]);
                    foreach (var val in bytes)
                        buffer[i++] = val;
                } else {
                    throw new InvalidDataException("Decimal.GetBytes() did not give us 128 bits worth of information.");
                }
            }

            return buffer;
        }

        /// <summary>
        /// Converts a little-endian byte-array into a decimal.
        /// </summary>
        /// <param name="buffer">The array to convert.</param>
        /// <param name="startIndex">The index to start reading bytes from.</param>
        /// <returns>loadedValue</returns>
        /// <exception cref="InvalidDataException">Thrown if there is not enough data in the buffer array.</exception>
        public static decimal ConvertByteArrayToDecimal(byte[] buffer, int startIndex = 0) {
            VerifyReadOk(buffer, startIndex, 16);

            int[] bits = new int[4];
            for (int i = 0; i <= 15; i += 4)
                bits[i / 4] = BitConverter.ToInt32(buffer, startIndex + i);

            return new decimal(bits);
        }


        /// <summary>
        /// Turns a byte[] into a string which can be displayed in the console in a similar style to a hex editor.
        /// </summary>
        /// <param name="buffer">The byte[] to display.</param>
        /// <param name="includeText">Whether or not a text view should be included side-by-side with the hex view.</param>
        /// <param name="bytesPerLine">The number of bytes to display per-line.</param>
        /// <returns>hexDumpStr</returns>
        public static string GetHexDump(byte[] buffer, bool includeText = true, int bytesPerLine = 8) {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < buffer.Length; i += bytesPerLine) {
                for (int j = 0; j < bytesPerLine; j++) {
                    int index = i + j;
                    builder.Append(buffer.Length > index ? buffer[index].ToString("X2") : "  ");
                    builder.Append(' ');
                }

                if (includeText) {
                    builder.Append(" |  ");
                    for (int j = 0; j < bytesPerLine; j++) {
                        int index = i + j;

                        if (buffer.Length > index) {
                            char val = (char)buffer[index];
                            builder.Append((val >= ' ' && val <= '~') || (val >= 127 && val <= 254) ? val : '?');
                        }
                    }
                }

                builder.Append(Environment.NewLine);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Convert a byte[] to a sequential string.
        /// byte[] arr = {0x01, 0x23, 0x34};
        /// Will be turned into "01 23 34".
        /// </summary>
        /// <param name="array">The byte array to convert.</param>
        /// <returns>byteStr</returns>
        public static string ToString(byte[] array) {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < array.Length; i++) {
                if (i > 0)
                    builder.Append(' ');
                builder.Append(array[i].ToString("X2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Test if the bytes at a given position match.
        /// </summary>
        /// <param name="testData">The data being tested.</param>
        /// <param name="expectedData">The expected data.</param>
        /// <param name="startIndex">The index to start checking the testData at.</param>
        /// <param name="encoding">Optional. The encoding to turn the string into bytes. Defaults to ASCII.</param>
        /// <returns>DoBytesMatch</returns>
        public static bool DoBytesMatch(byte[] testData, string expectedData, int startIndex = 0, Encoding encoding = null) {
            return DoBytesMatch(testData, (encoding ?? Encoding.ASCII).GetBytes(expectedData), startIndex);
        }

        /// <summary>
        /// Test if the bytes at a given position match.
        /// Throws an exception if they don't.
        /// </summary>
        /// <param name="failMessage">The message to display if they don't match.</param>
        /// <param name="testData">The data being tested.</param>
        /// <param name="expectedData">The expected data.</param>
        /// <param name="startIndex">The index to start checking the testData at.</param>
        /// <param name="encoding">Optional. The encoding to turn the string into bytes. Defaults to ASCII.</param>
        /// <returns>DoBytesMatch</returns>
        public static void VerifyBytesMatch(string failMessage, byte[] testData, string expectedData, int startIndex = 0, Encoding encoding = null) {
            VerifyBytesMatch(failMessage, testData, (encoding ?? Encoding.ASCII).GetBytes(expectedData), startIndex);
        }

        /// <summary>
        /// Test if a byte array has bytes which match another array at a given index.
        /// </summary>
        /// <param name="testData">The data to test.</param>
        /// <param name="expectedData">The expected data.</param>
        /// <param name="startIndex">The index to start the comparison at in the testData.</param>
        /// <returns>DoBytesMatch</returns>
        public static bool DoBytesMatch(byte[] testData, byte[] expectedData, int startIndex = 0) {
            if (expectedData.Length > testData.Length - startIndex)
                return false; // There isn't enough bytes, so it can't match.

            for (int i = 0; i < expectedData.Length; i++)
                if (testData[startIndex + i] != expectedData[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Test if a byte array has bytes which match another array at a given index.
        /// Throws an exception if they do not match.
        /// </summary>
        /// <param name="failMessage">The message to display if they do not match.</param>
        /// <param name="testData">The data to test.</param>
        /// <param name="expectedData">The expected data.</param>
        /// <param name="startIndex">The index to start the comparison at in the testData.</param>
        public static void VerifyBytesMatch(string failMessage, byte[] testData, byte[] expectedData, int startIndex = 0) {
            if (expectedData.Length > testData.Length - startIndex)
                throw new InvalidDataException(failMessage + " (Length Mismatch)\n" 
                                                           + "Test Data: [" + ToString(testData) + "]\n"
                                                           + "Expected:  [" + ToString(expectedData) + "]");

            for (int i = 0; i < expectedData.Length; i++)
                if (testData[startIndex + i] != expectedData[i])
                    throw new InvalidDataException(failMessage + " (Byte Mismatch @ " + i + ")\n"
                                                   + "Test Data: [" + ToString(testData) + "]\n"
                                                   + "Expected:  [" + ToString(expectedData) + "]");
        }

        /// <summary>
        /// Format a number of bytes as a user friendly string. Eg. 2048 -> "2 Kb"
        /// Note: If a decimal position is omitted, the result will be rounding. For instance, 9.57 MB will get rounded to 10MB.
        /// </summary>
        /// <param name="size">The number of bytes to format.</param>
        /// <param name="decimalDigits">The number of decimal digits to show. 2 -> "2.34 Kb"</param>
        /// <returns>formattedString</returns>
        public static string ConvertByteCountToFileSize(ulong size, uint decimalDigits = 0) {
            string[] names = Enum.GetNames(typeof(DataUnit));

            ulong unit = 1;
            foreach (string unitName in names) {
                if ((unit * 1024) >= size) {
                    decimal unitValue = (decimal)size / unit;
                    return unitValue.ToString($"N{decimalDigits}") + " " + unitName;
                }

                unit *= 1024;
            }

            unit /= 1024;
            return ((decimal)size / unit).ToString($"N{decimalDigits}") + " " + names[^1];
        }

        /// <summary>
        /// Reads an object from a DataReader.
        /// </summary>
        /// <param name="reader">The reader to read the object from.</param>
        /// <param name="settings">The settings to read the object with. (Can be null)</param>
        /// <typeparam name="TData">The type of the object to read.</typeparam>
        /// <returns>readObject</returns>
        public static TData ReadObjectFromReader<TData>(DataReader reader, DataSettings settings = null) where TData : IBinarySerializable, new() {
            TData newResult = new TData();
            newResult.LoadFromReader(reader, settings);
            return newResult;
        }
    }
}