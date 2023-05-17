using ModToolFramework.Utils.DataStructures.Number;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;

namespace ModToolFramework.Utils.Data {
    /// <summary>
    /// An upgraded BinaryWriter with extra utilities useful when modding.
    /// NOTE: This is an IDisposable, so this should always be disposed when it's done being used.
    /// I would call this BinaryWriter, but we gotta distinguish it from System.IO.BinaryWriter, since System.IO does need to be loaded for System.IO.Stream when you want to instantiate the class. Theoretically you could type out the full names, but I think it's one step too far.
    /// </summary>
    public class DataWriter : BinaryWriter {
        private byte[] _buffer = new byte[16];
        private Stack<long> _jumpStack = new Stack<long>();

        public DataWriter(Stream outStream) : base(outStream) {
        }

        public DataWriter(Stream outStream, bool leaveOpen) : base(outStream, Encoding.ASCII, leaveOpen) {
        }

        /// <summary>
        /// The position which the writer will write its next byte to. The underlying stream position.
        /// </summary>
        public virtual long Index { get => this.OutStream.Position; set => this.OutStream.Position = value; }
        
        /// <summary>
        /// The endian which the written values should be encoded with. Defaults to little endian.
        /// </summary>
        public ByteEndian Endian { get; set; } = ByteEndian.LittleEndian;

        /// <summary>
        /// A writer which writes to a source that doesn't receive anything. Main purpose is for counting bytes written.
        /// Should be locked whenever accessed to ensure thread-safety.
        /// </summary>
        public static DataWriter NullWriter { get; } = new DataWriter(Stream.Null, true);

        /// <summary>
        /// Push the current index onto a jump stack, so you can return to the current index later with JumpReturn().
        /// Updates the current index to the supplied one.
        /// </summary>
        /// <param name="newIndex">The new index to move to.</param>
        public virtual void JumpTemp(long newIndex) {
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
        /// Writes an int32 to a given index.
        /// </summary>
        /// <param name="index">The index to write the value to.</param>
        /// <param name="value">The value to write.</param>
        public virtual void WriteAt(long index, int value) {
            this.JumpTemp(index);
            this.Write(value);
            this.JumpReturn();
        }

        /// <summary>
        /// Writes a uint32 to a given index.
        /// </summary>
        /// <param name="index">The index to write the value to.</param>
        /// <param name="value">The value to write.</param>
        public virtual void WriteAt(long index, uint value) {
            this.JumpTemp(index);
            this.Write(value);
            this.JumpReturn();
        }

        /// <summary>
        /// Writes a long to a given index.
        /// </summary>
        /// <param name="index">The index to write the value to.</param>
        /// <param name="value">The value to write.</param>
        public virtual void WriteAt(long index, long value) {
            this.JumpTemp(index);
            this.Write(value);
            this.JumpReturn();
        }

        /// <summary>
        /// Writes the current index to a supplied address/index.
        /// </summary>
        /// <param name="index">The index to write data to.</param>
        public void WriteAddress32At(long index) {
            if (this.Index > int.MaxValue) // The index is beyond 2GB, or what can be represented with an int.
                throw new InvalidDataException("Current index is larger than 32-bit address mode.");

            unchecked {
                WriteAt(index, (uint)this.Index);
            }
        }

        /// <summary>
        /// Writes the current index to a supplied address/index.
        /// </summary>
        /// <param name="index">The index to write data to.</param>
        public void WriteAddress64At(long index) {
            this.WriteAt(index, this.Index);
        }

        /// <summary>
        /// Write a given number of null (0x00) bytes.
        /// </summary>
        /// <param name="amount">The amount of null bytes to write.</param>
        /// <returns>The writer's index before the data is written.</returns>
        public virtual long WriteNullBytes(int amount) {
            long returnIndex = this.Index;
            for (int i = 0; i < amount; i++)
                this.Write((byte)0x00);
            return returnIndex;
        }

        /// <summary>
        /// Writes null-bytes until the current index reaches a supplied index.
        /// </summary>
        /// <param name="endIndex">The index to write null bytes until.</param>
        public virtual void WriteNullUntil(int endIndex) {
            this.WriteBytesUntil(endIndex, 0x00);
        }

        /// <summary>
        /// Writes bytes until the current index reaches a given index.
        /// </summary>
        /// <param name="endIndex">The index to write bytes until.</param>
        /// <param name="value">The byte to write.</param>
        public virtual void WriteBytesUntil(long endIndex, byte value) {
            for (long i = this.Index; i < endIndex; i++)
                this.Write(value);
        }

        /// <summary>
        /// Writes a 32bit null pointer to the output.
        /// </summary>
        /// <returns>The index that the pointer was written to.</returns>
        public virtual long WriteNullPointer32() {
            long returnIndex = this.Index;
            for (int i = 0; i < 4; i++)
                this.Write((byte)0x00);
            return returnIndex;
        }

        /// <summary>
        /// Writes a 64bit null pointer to the output.
        /// </summary>
        /// <returns>The index that the pointer was written to.</returns>
        public virtual long WriteNullPointer64() {
            long returnIndex = this.Index;
            for (int i = 0; i < 8; i++)
                this.Write((byte)0x00);
            return returnIndex;
        }

        /// <summary>
        /// Writes the bytes of a string to the output.
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <param name="encoding">The encoding to write the string with.</param>
        public virtual void WriteStringBytes(string value, Encoding encoding = null) {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] bytes = (encoding ?? Encoding.ASCII).GetBytes(value);
            this.Write(bytes);
        }

        /// <summary>
        /// Writes a null-terminated string to the output.
        /// </summary>
        /// <param name="value">The string to write</param>
        /// <param name="encoding">The encoding to use. Null will default to ASCII.</param>
        public virtual void WriteNullTerminatedString(string value, Encoding encoding = null) {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            this.WriteStringBytes(value, encoding);
            this.Write((byte)0x00);
        }

        /// <summary>
        /// Writes a string into a fixed-size
        /// </summary>
        /// <param name="value">The string to write.</param>
        /// <param name="fixedSize">The fixed size, or the max size of the string.</param>
        /// <param name="terminator">The terminator character which goes at the end of the string.</param>
        /// <param name="padding">The padding data which goes after the terminator to the end of the string byte area.</param>
        /// <param name="encoding">The encoding to encode the string as. Null will default to ascii.</param>
        public virtual void WriteFixedSizeString(string value, int fixedSize, char terminator = '\0', byte padding = 0x00, Encoding encoding = null) {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            byte[] bytes = (encoding ?? Encoding.ASCII).GetBytes(value);
            if (bytes.Length > fixedSize)
                throw new InvalidDataException("String '" + value + "' did not fit within the given size. (" + bytes.Length + " > " + fixedSize + ")");

            long endIndex = this.Index + fixedSize;
            this.Write(bytes);
            if (endIndex > this.Index) {
                this.Write(terminator); // Don't write the terminator if it's the exact size.
                for (long i = this.Index; i < endIndex; i++)
                    this.Write(padding);
            }
        }

        private void WriteBufferWithEndian(int length) {
            if (this.Endian == ByteEndian.BigEndian) { // Handle big endian-swapping.
                for (int i = 0; i < length / 2; i++) {
                    byte temp = this._buffer[i];
                    this._buffer[i] = this._buffer[length - i - 1];
                    this._buffer[length - i - 1] = temp;
                }
            }

            this.Write(this._buffer, 0, length);
        }

        // Beyond here overrides the default functionality.
        // It adds support for BigEndian.
        // Everything past this needs to implement Index incrementing, even if it's handled indirectly (For instance, calling WriteBufferWithEndian)

        /// <inheritdoc cref="BinaryWriter.Write(ulong)"/>
        public override void Write(ulong value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this._buffer[2] = (byte)(value >> 16);
            this._buffer[3] = (byte)(value >> 24);
            this._buffer[4] = (byte)(value >> 32);
            this._buffer[5] = (byte)(value >> 40);
            this._buffer[6] = (byte)(value >> 48);
            this._buffer[7] = (byte)(value >> 56);
            this.WriteBufferWithEndian(8); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(uint)"/>
        public override void Write(uint value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this._buffer[2] = (byte)(value >> 16);
            this._buffer[3] = (byte)(value >> 24);
            this.WriteBufferWithEndian(4); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(ushort)"/>
        public override void Write(ushort value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this.WriteBufferWithEndian(2); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(long)"/>
        public override void Write(long value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this._buffer[2] = (byte)(value >> 16);
            this._buffer[3] = (byte)(value >> 24);
            this._buffer[4] = (byte)(value >> 32);
            this._buffer[5] = (byte)(value >> 40);
            this._buffer[6] = (byte)(value >> 48);
            this._buffer[7] = (byte)(value >> 56);
            this.WriteBufferWithEndian(8); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(int)"/>
        public override void Write(int value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this._buffer[2] = (byte)(value >> 16);
            this._buffer[3] = (byte)(value >> 24);
            this.WriteBufferWithEndian(4); // Increments index.
        }
        
        /// <summary>
        /// Writes a signed 8-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(FixedPoint8 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes an unsigned 8-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(UFixedPoint8 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes a signed 16-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(FixedPoint16 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes an unsigned 16-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(UFixedPoint16 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes a signed 32-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(FixedPoint32 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes an unsigned 32-bit fixed point number.
        /// </summary>
        /// <param name="value">The number to write.</param>
        public void Write(UFixedPoint32 value) {
            this.Write(value.FixedValue);
        }
        
        /// <summary>
        /// Writes a half (16 bit float) value to the stream.
        /// </summary>
        /// <param name="value">The half value to write.</param>
        [SecuritySafeCritical]
        public void Write(Half value) {
            Unsafe.As<byte, Half>(ref this._buffer[0]) = value;
            this.WriteBufferWithEndian(2); // Increments index.
        }
        
        /// <inheritdoc cref="BinaryWriter.Write(float)"/>
        [SecuritySafeCritical]
        public override unsafe void Write(float value) {
            uint tmpValue = *(uint*)&value; // Yo this is awesome, I didn't know C# could do this.
            this._buffer[0] = (byte)tmpValue;
            this._buffer[1] = (byte)(tmpValue >> 8);
            this._buffer[2] = (byte)(tmpValue >> 16);
            this._buffer[3] = (byte)(tmpValue >> 24);
            this.WriteBufferWithEndian(4); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(double)"/>
        [SecuritySafeCritical]
        public override unsafe void Write(double value) {
            ulong tmpValue = *(ulong*)&value;
            this._buffer[0] = (byte)tmpValue;
            this._buffer[1] = (byte)(tmpValue >> 8);
            this._buffer[2] = (byte)(tmpValue >> 16);
            this._buffer[3] = (byte)(tmpValue >> 24);
            this._buffer[4] = (byte)(tmpValue >> 32);
            this._buffer[5] = (byte)(tmpValue >> 40);
            this._buffer[6] = (byte)(tmpValue >> 48);
            this._buffer[7] = (byte)(tmpValue >> 56);
            this.WriteBufferWithEndian(8); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Write(decimal)"/>
        public override void Write(decimal value) {
            int[] bits = decimal.GetBits(value);

            int i = 0;
            int j = 0;
            while (i < 16) {
                if (bits.Length > j) {
                    byte[] bytes = BitConverter.GetBytes(bits[j++]);
                    foreach (byte val in bytes)
                        this._buffer[i++] = val;
                } else {
                    throw new InvalidDataException("Decimal.GetBytes() did not give us 128 bits worth of information.");
                }
            }

            this.WriteBufferWithEndian(16);
        }

        /// <inheritdoc cref="BinaryWriter.Write(short)"/>
        public override void Write(short value) {
            this._buffer[0] = (byte)value;
            this._buffer[1] = (byte)(value >> 8);
            this.WriteBufferWithEndian(2); // Increments index.
        }

        /// <inheritdoc cref="BinaryWriter.Dispose"/>
        protected override void Dispose(bool dispose) {
            base.Dispose(dispose);
            this._buffer = null;
            this._jumpStack = null;
        }
    }
}