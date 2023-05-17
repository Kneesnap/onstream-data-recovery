using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ModToolFramework.Utils.Data {
    /// <summary>
    /// An enhanced version of System.IO.StringReader containing extra utilities.
    /// An interface extending TextReader was considered, however it was determined that was deemed redundant because DataReader can do everything this can for binary string data.
    /// </summary>
    public class EnhancedStringReader : StringReader {
        private static readonly Func<StringReader, int> PosGetter = DynamicCodeUtils.CreateGetter<StringReader, int>("_pos");
        private static readonly Action<StringReader, int> PosSetter = DynamicCodeUtils.CreateSetter<StringReader, int>("_pos");
        private Stack<int> _jumpStack = new Stack<int>();
        /// <summary>
        /// The current position in the string being read. Increasing will skip characters. Decreasing will take to previously seen characters.
        /// </summary>
        public int Index { get => PosGetter.Invoke(this); set => PosSetter.Invoke(this, value); }
        
        /// <summary>
        /// The string being read by this reader.
        /// </summary>
        public string String { get; private set; }
        
        /// <summary>
        /// The size of the string being read.
        /// </summary>
        public int Size { get; private set; }
        /// <summary>
        /// How many characters/bytes are left to read.
        /// </summary>
        public int Remaining => Size - Index;
        /// <summary>
        /// Whether or not there are any characters left to read.
        /// </summary>
        public bool HasMore => Remaining > 0;

        public EnhancedStringReader(string s) : base(s) {
            this.String = s;
            this.Size = s.Length;
        }

        /// <summary>
        /// Peeks the next character.
        /// </summary>
        /// <returns>peekedChar</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is no next character.</exception>
        public char PeekChar() {
            int next = this.Peek();
            if (next == -1)
                throw new EndOfStreamException("The reader has no more data to read from.");
            if (next < 0 || next > 255)
                throw new InvalidDataException($"An unexpected value was read. ({next})");
            return (char)next;
        }

        /// <summary>
        /// Reads the next character.
        /// </summary>
        /// <returns>readChar</returns>
        /// <exception cref="EndOfStreamException">Thrown if there is no next character.</exception>
        public char ReadChar() {
            int next = this.Read();
            if (next == -1)
                throw new EndOfStreamException("The reader has no more data to read from.");
            if (next < 0 || next > 255)
                throw new InvalidDataException($"An unexpected value was read. ({next})");
            return (char)next;
        }

        /// <summary>
        /// Push the current index onto a jump stack, so you can return to the current index later with JumpReturn().
        /// Updates the current index to the supplied one.
        /// </summary>
        /// <param name="newIndex">The new index to move to.</param>
        public virtual void JumpTemp(int newIndex) {
            this._jumpStack.Push(this.Index);
            this.Index = newIndex;
        }

        /// <summary>
        /// Return to the last index that JumpTemp was called from.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if there is no index which can be returned to.</exception>
        public virtual void JumpReturn() {
            if (this._jumpStack.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(this._jumpStack), "Could not return to previous position, the JumpStack was empty.");
            this.Index = this._jumpStack.Pop();
        }

        /// <summary>
        /// Skip a given number of bytes.
        /// </summary>
        /// <param name="byteCount">The number of bytes to skip.</param>
        public virtual void SkipChars(int byteCount) {
            this.Index += byteCount;
        }

        /// <summary>
        /// Verify a string matches the raw stream.
        /// </summary>
        /// <param name="verify">The stream to verify.</param>
        /// <param name="encoding">The encoding to use. Uses ASCII if null is specified.</param>
        public virtual void VerifyString(string verify, Encoding encoding = null) {
            encoding ??= Encoding.ASCII;
            char[] testAgainst = encoding.GetChars(encoding.GetBytes(verify));
            char[] readChars = new char[testAgainst.Length];
            this.Read(readChars, 0, readChars.Length);
            if (!readChars.SequenceEqual(testAgainst))
                throw new Exception("String verification failed. Expected: \"" + verify + "\", Got: \"" + encoding.GetString(encoding.GetBytes(readChars)) + "\".");
        }

        /// <summary>
        /// Reads the next line.
        /// Throws an exception if there is no next line. (Use HasMore to check.)
        /// This overrides the base method so it no longer returns "string?", but rather a "string".
        /// This is preferred behavior for ModToolFramework use-cases.
        /// </summary>
        /// <returns>readLine</returns>
        /// <exception cref="EndOfStreamException">Thrown if it is not possible to read the next string.</exception>
        public override string ReadLine() {
            string returnValue = base.ReadLine();
            if (returnValue == null) {
                if (!this.HasMore) 
                    throw new EndOfStreamException("Could not read next line, reached end of stream.");
                throw new Exception("Could not read next line, unknown reason.");
            }

            return returnValue;
        }

        /// <summary>
        /// Reads a string from raw bytes.
        /// </summary>
        /// <param name="length">The number of chars to read.</param>
        /// <param name="encoding">The string's encoding. Null will default to ASCII.</param>
        /// <returns>loadedString</returns>
        public virtual string ReadString(int length, Encoding encoding = null) {
            encoding ??= Encoding.ASCII;
            char[] readChars = new char[length];
            this.Read(readChars, 0, length);
            return encoding.GetString(encoding.GetBytes(readChars));
        }

        /// <inheritdoc cref="System.IO.StringReader"/>
        protected override void Dispose(bool disposing) {
            this.String = null;
            this.Size = 0;
            this._jumpStack.Clear();
            this._jumpStack = null;
            base.Dispose(disposing);
        }
    }
}