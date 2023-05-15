using System;
using System.Text;

namespace RetrospectTape.MacEncoding
{
    /// <summary>Implements a class that converts to/from a single byte codepage and UTF-16 representable strings</summary>
    public abstract class SingleByteEncoding : Encoding
    {
        /// <summary>Character conversion table</summary>
        protected abstract char[] CharTable { get; }

        /// <summary>Gets a value indicating whether the current encoding can be used by browser clients for displaying content.</summary>
        public abstract override bool IsBrowserDisplay { get; }

        /// <summary>Gets a value indicating whether the current encoding can be used by browser clients for saving content.</summary>
        public abstract override bool IsBrowserSave { get; }

        /// <summary>
        ///     Gets a value indicating whether the current encoding can be used by mail and news clients for displaying
        ///     content.
        /// </summary>
        public abstract override bool IsMailNewsDisplay { get; }

        /// <summary>Gets a value indicating whether the current encoding can be used by mail and news clients for saving content.</summary>
        public abstract override bool IsMailNewsSave { get; }

        /// <summary>Gets a value indicating whether the current encoding uses single-byte code points.</summary>
        public abstract override bool IsSingleByte { get; }

        /// <summary>Gets the code page identifier of the current Encoding.</summary>
        public abstract override int CodePage { get; }

        /// <summary>Gets a name for the current encoding that can be used with mail agent body tags</summary>
        public abstract override string BodyName { get; }

        /// <summary>Gets a name for the current encoding that can be used with mail agent header tags</summary>
        public abstract override string HeaderName { get; }

        /// <summary>Gets the name registered with the Internet Assigned Numbers Authority (IANA) for the current encoding.</summary>
        public abstract override string WebName { get; }

        /// <summary>Gets the human-readable description of the current encoding.</summary>
        public abstract override string EncodingName { get; }

        /// <summary>Gets the Windows operating system code page that most closely corresponds to the current encoding.</summary>
        public abstract override int WindowsCodePage { get; }

        /// <summary>Calculates the number of bytes produced by encoding the characters in the specified <see cref="string" />.</summary>
        /// <returns>The number of bytes produced by encoding the specified characters.</returns>
        /// <param name="s">The <see cref="string" /> containing the set of characters to encode.</param>
        public override int GetByteCount(string s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            return s.Length;
        }

        /// <summary>Calculates the number of bytes produced by encoding a set of characters from the specified character array.</summary>
        /// <returns>The number of bytes produced by encoding the specified characters.</returns>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="index">The index of the first character to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        public override int GetByteCount(char[] chars, int index, int count) {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (index < 0 ||
                index >= chars.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0 ||
                index + count > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return count;
        }

        /// <summary>Calculates the number of bytes produced by encoding all the characters in the specified character array.</summary>
        /// <returns>The number of bytes produced by encoding all the characters in the specified character array.</returns>
        /// <param name="chars">The character array containing the characters to encode.</param>
        public override int GetByteCount(char[] chars) {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            return chars.Length;
        }

        /// <summary>Encodes a set of characters from the specified <see cref="string" /> into the specified byte array.</summary>
        /// <returns>The actual number of bytes written into bytes.</returns>
        /// <param name="s">The <see cref="string" /> containing the set of characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) =>
            GetBytes(s.ToCharArray(), charIndex, charCount, bytes, byteIndex);

        /// <summary>Encodes all the characters in the specified string into a sequence of bytes.</summary>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <param name="s">The string containing the characters to encode.</param>
        public override byte[] GetBytes(string s) {
            if (s == null)
                throw new ArgumentNullException(nameof(s));

            return GetBytes(s.ToCharArray(), 0, s.Length);
        }

        /// <summary>Encodes a set of characters from the specified character array into the specified byte array.</summary>
        /// <returns>The actual number of bytes written into bytes.</returns>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="charIndex">The index of the first character to encode.</param>
        /// <param name="charCount">The number of characters to encode.</param>
        /// <param name="bytes">The byte array to contain the resulting sequence of bytes.</param>
        /// <param name="byteIndex">The index at which to start writing the resulting sequence of bytes.</param>
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (charIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(charIndex));

            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            if (byteIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (charIndex >= chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex));

            if (charCount + charIndex > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            if (byteIndex >= bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (byteIndex + charCount > bytes.Length)
                throw new ArgumentException(nameof(bytes));

            byte[] temp = GetBytes(chars, charIndex, charCount);

            for (int i = 0; i < temp.Length; i++)
                bytes[i + byteIndex] = temp[i];

            return charCount;
        }

        /// <summary>Encodes a set of characters from the specified character array into a sequence of bytes.</summary>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <param name="chars">The character array containing the set of characters to encode.</param>
        /// <param name="index">The index of the first character to encode.</param>
        /// <param name="count">The number of characters to encode.</param>
        public override byte[] GetBytes(char[] chars, int index, int count) {
            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count + index > chars.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            byte[] bytes = new byte[count];

            for (int i = 0; i < count; i++)
                bytes[i] = GetByte(chars[index + i]);

            return bytes;
        }

        /// <summary>Encodes all the characters in the specified character array into a sequence of bytes.</summary>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        /// <param name="chars">The character array containing the characters to encode.</param>
        public override byte[] GetBytes(char[] chars) => GetBytes(chars, 0, chars.Length);

        /// <summary>Calculates the number of characters produced by decoding all the bytes in the specified byte array.</summary>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        public override int GetCharCount(byte[] bytes) => GetCharCount(bytes, 0, bytes.Length);

        /// <summary>Calculates the number of characters produced by decoding a sequence of bytes from the specified byte array.</summary>
        /// <returns>The number of characters produced by decoding the specified sequence of bytes.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        public override int GetCharCount(byte[] bytes, int index, int count) {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count + index > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            return count;
        }

        /// <summary>Decodes a sequence of bytes from the specified byte array into the specified character array.</summary>
        /// <returns>The actual number of characters written into chars.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="byteIndex">The index of the first byte to decode.</param>
        /// <param name="byteCount">The number of bytes to decode.</param>
        /// <param name="chars">The character array to contain the resulting set of characters.</param>
        /// <param name="charIndex">The index at which to start writing the resulting set of characters.</param>
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (chars == null)
                throw new ArgumentNullException(nameof(chars));

            if (byteIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            if (charIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(charIndex));

            if (byteIndex >= bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (byteCount + byteIndex > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            if (charIndex >= chars.Length)
                throw new ArgumentOutOfRangeException(nameof(charIndex));

            if (charIndex + byteCount > chars.Length)
                throw new ArgumentException(nameof(chars));

            char[] temp = GetChars(bytes, byteIndex, byteCount);

            for (int i = 0; i < temp.Length; i++)
                chars[i + charIndex] = temp[i];

            return byteCount;
        }

        /// <summary>Decodes all the bytes in the specified byte array into a set of characters.</summary>
        /// <returns>A character array containing the results of decoding the specified sequence of bytes.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        public override char[] GetChars(byte[] bytes) => GetChars(bytes, 0, bytes.Length);

        /// <summary>Decodes a sequence of bytes from the specified byte array into a set of characters.</summary>
        /// <returns>The chars.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        public override char[] GetChars(byte[] bytes, int index, int count) {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (count + index > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            char[] chars = new char[count];

            for (int i = 0; i < count; i++)
                chars[i] = GetChar(bytes[index + i]);

            return chars;
        }

        /// <summary>Calculates the maximum number of bytes produced by encoding the specified number of characters.</summary>
        /// <returns>The maximum number of bytes produced by encoding the specified number of characters.</returns>
        /// <param name="charCount">The number of characters to encode.</param>
        public override int GetMaxByteCount(int charCount) {
            if (charCount < 0)
                throw new ArgumentOutOfRangeException(nameof(charCount));

            return charCount;
        }

        /// <summary>Calculates the maximum number of characters produced by decoding the specified number of bytes.</summary>
        /// <returns>The maximum number of characters produced by decoding the specified number of bytes.</returns>
        /// <param name="byteCount">The number of bytes to decode.</param>
        public override int GetMaxCharCount(int byteCount) {
            if (byteCount < 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount));

            return byteCount;
        }

        /// <summary>Returns a sequence of bytes that specifies the encoding used.</summary>
        /// <returns>A byte array of length zero, as a preamble is not required.</returns>
        public override byte[] GetPreamble() => new byte[0];

        /// <summary>Decodes all the bytes in the specified byte array into a string.</summary>
        /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        public override string GetString(byte[] bytes) => GetString(bytes, 0, bytes.Length);

        /// <summary>Decodes a sequence of bytes from the specified byte array into a string.</summary>
        /// <returns>A string that contains the results of decoding the specified sequence of bytes.</returns>
        /// <param name="bytes">The byte array containing the sequence of bytes to decode.</param>
        /// <param name="index">The index of the first byte to decode.</param>
        /// <param name="count">The number of bytes to decode.</param>
        public override string GetString(byte[] bytes, int index, int count) => new(GetChars(bytes, index, count));

        /// <summary>Converts a codepage character to an Unicode character</summary>
        /// <returns>Unicode character.</returns>
        /// <param name="character">Codepage character.</param>
        char GetChar(byte character) => CharTable[character];

        private protected abstract byte GetByte(char character);
    }
}