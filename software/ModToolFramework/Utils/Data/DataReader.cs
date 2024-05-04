using ModToolFramework.Utils.DataStructures.Number;
using ModToolFramework.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace ModToolFramework.Utils.Data
{
    /// <summary>
    /// An upgraded BinaryReader with extra utilities useful when modding.
    /// NOTE: This is an IDisposable, so this should always be disposed when it's done being used.
    /// I would call this BinaryReader, but we gotta distinguish it from System.IO.BinaryReader, since System.IO does need to be loaded for System.IO.Stream when you want to instantiate the class. Theoretically you could type out the full names, but I think it's one step too far.
    /// Should work on files larger than 2GB.
    /// </summary>
    public class DataReader : BinaryReader
    {
        private Stack<long> _jumpStack = new Stack<long>();
        private byte[] _buffer = new byte[16];
        public ByteEndian Endian { get; set; } = ByteEndian.LittleEndian;
        public long Index { get => this.BaseStream.Position; set => this.BaseStream.Position = value; }
        public long Size { get => this.BaseStream.Length; }
        public long Remaining { get => this.Size - this.Index; }
        public bool HasMore { get => (this.Remaining > 0); }

        public DataReader(Stream inStream, bool leaveOpen = false) : base(inStream, Encoding.UTF8, leaveOpen) {
        }
        
        /// <summary>
        /// Skip bytes, requiring the bytes skipped be 0.
        /// </summary>
        /// <param name="amount">The number of bytes to skip</param>
        public void SkipBytesRequireEmpty(int amount) {
            SkipBytesRequire(DataConstants.NullByte, amount);
        }
        
        /// <summary>
        /// Skip bytes, requiring the bytes skipped be the expected value.
        /// </summary>
        /// <param name="expected">The value to require</param>
        /// <param name="amount">The amount of bytes to skip</param>
        /// <exception cref="Exception">Thrown if some of the bytes are not expected</exception>
        public void SkipBytesRequire(byte expected, int amount) {
            long index = this.Index;
            if (amount == 0)
                return;

            if (amount < 0)
                throw new Exception($"Tried to skip {amount} bytes.");

            // Skip bytes.
            for (int i = 0; i < amount; i++) {
                byte nextByte = ReadByte();
                if (nextByte != expected)
                    throw new Exception($"Reader wanted to skip {amount} bytes to reach 0x{(index + amount):X8}, but got 0x{nextByte:X2} at {(index + i):X} when 0x{expected:X2} was desired.");
            }
        }

        /// <summary>
        /// Align the reader to the next multiple of the provided value.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <returns>The number of bytes skipped.</returns>
        public long Align(long interval) {
            long difference = (this.Index % interval);
            if (difference == 0)
                return 0;

            long byteIncrease = (interval - difference); 
            this.Index += byteIncrease;
            return byteIncrease;
        }

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
        /// Skip a given number of bytes.
        /// </summary>
        /// <param name="byteCount">The number of bytes to skip.</param>
        public virtual void SkipBytes(int byteCount) {
            if (byteCount != 0) 
                this.Index += byteCount;
        }

        /// <summary>
        /// Verify a string matches the raw stream.
        /// </summary>
        /// <param name="verify">The string to verify.</param>
        /// <param name="encoding">The encoding to use. Uses ASCII if null is specified.</param>
        public virtual void VerifyStringBytes(string verify, Encoding encoding = null) {
            byte[] testAgainst = (encoding ?? Encoding.ASCII).GetBytes(verify);
            byte[] readBytes = this.ReadBytes(testAgainst.Length);
            if (!readBytes.SequenceEqual(testAgainst))
                throw new Exception("String verification failed. Expected: \"" + verify + "\", Got: \"" + (encoding ?? Encoding.ASCII).GetString(readBytes) + "\".");
        }
        
        /// <summary>
        /// Verify a byte[] matches the raw stream.
        /// </summary>
        /// <param name="testAgainst">The bytes to verify.</param>
        public virtual void VerifyBytes(byte[] testAgainst) {
            byte[] readBytes = this.ReadBytes(testAgainst.Length);
            if (!readBytes.SequenceEqual(testAgainst))
                throw new Exception($"String verification failed. Expected: \"{DataUtils.ToString(testAgainst)}\", Got: \"{DataUtils.ToString(readBytes)}\".");
        }

        /// <summary>
        /// Reads data as text until a newline (\n) is read.
        /// </summary>
        /// <param name="encoding">The text encoding to use. Defaults to ascii.</param>
        /// <returns>textLine</returns>
        public string ReadLine(Encoding encoding = null) {
            List<byte> readBytes = new List<byte>();

            bool lastWasBackslashR = false;
            while (this.HasMore) {
                byte nextByte = this.ReadByte();
                
                if (lastWasBackslashR && nextByte != 0x0A) {
                    lastWasBackslashR = false;
                    readBytes.Add(0x0D);
                }
                
                if (nextByte == 0x0D) { // '\r' (Like in '\r\n'.
                    lastWasBackslashR = true;
                } else if (nextByte == 0x00 || nextByte == 0x0A) {
                    break;
                } else {
                    readBytes.Add(nextByte);
                }
            }

            byte[] stringData = readBytes.ToArray<byte>();
            return (encoding ?? Encoding.ASCII).GetString(stringData);
        }

        /// <summary>
        /// Reads a string from raw bytes.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="encoding">The string's encoding. Null will default to ASCII.</param>
        /// <returns>loadedString</returns>
        public virtual string ReadStringBytes(int length, Encoding encoding = null) {
            byte[] readBytes = this.ReadBytes(length);
            return (encoding ?? Encoding.ASCII).GetString(readBytes);
        }

        /// <summary>
        /// Reads a null-terminated string from the stream.
        /// </summary>
        /// <param name="encoding">The string's encoding.</param>
        /// <returns>loadedString</returns>
        public virtual string ReadNullTerminatedString(Encoding encoding = null) {
            bool foundEnd = false;
            List<byte> readBytes = new List<byte>();
            while (this.HasMore) {
                byte nextByte = this.ReadByte();
                if (nextByte == 0x00) { // TODO: Needs to handle non-normal encoding.
                    foundEnd = true;
                    break;
                } else {
                    readBytes.Add(nextByte);
                }
            }

            if (!foundEnd)
                throw new InvalidDataException("There was no null-terminator, all of the data was read.");

            byte[] stringData = readBytes.ToArray<byte>();
            return (encoding ?? Encoding.ASCII).GetString(stringData);
        }

        /// <summary>
        /// Reads a fixed-size string from the stream.
        /// </summary>
        /// <param name="fixedSize">The fixed size.</param>
        /// <param name="terminator">The early terminator.</param>
        /// <param name="encoding">What the string is encoded in, if null, it will default to ASCII.</param>
        /// <returns>readString</returns>
        public virtual string ReadFixedSizeString(int fixedSize, char terminator = '\0', Encoding encoding = null) {
            byte[] readData = this.ReadBytes(fixedSize);

            encoding ??= Encoding.ASCII;

            // Single byte reading is simple.
            if (encoding.IsSingleByte) {
                int foundIndex = -1;

                for (int i = 0; i < readData.Length; i++) {
                    if (readData[i] == (byte)terminator) {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex != -1) // Remove everything at the terminator or after.
                    readData = readData.CloneArray(0, foundIndex);
                return encoding.GetString(readData);
            }

            // Read specially spaced characters.
            using MemoryStream newStream = new MemoryStream(readData);
            using BinaryReader newReader = new BinaryReader(newStream, encoding);

            long startBytePos = newReader.BaseStream.Position;
            long endBytePos = startBytePos;
            int temp;
            while ((temp = newReader.Read()) != -1) {
                if ((char)temp == terminator)
                    break;

                endBytePos = newReader.BaseStream.Position;
            }

            int byteLength = (int) (endBytePos - startBytePos);
            if (readData.LongLength > byteLength) // Remove everything at the terminator or after.
                readData = readData.CloneArray(0, byteLength);

            return encoding.GetString(readData);
        }

        // These methods are implemented the way they are so big-endian can be supported.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadBytesWithEndian(int length) {
            this.ReadBytesWithEndian(length, this.Endian);
        }

        private void ReadBytesWithEndian(int length, ByteEndian endian) {
            int readAmount = this.BaseStream.Read(this._buffer, 0, length);
            if (readAmount != length)
                throw new IndexOutOfRangeException("Attempted to read data past the end of the stream.");

            if (endian == ByteEndian.BigEndian) {
                for (int i = 0; i < (length / 2); i++) {
                    byte temp = this._buffer[i];
                    this._buffer[i] = this._buffer[length - i - 1];
                    this._buffer[length - i - 1] = temp;
                }
            }
        }
        
        /// <summary>
        /// Reads a 16 bit float from the stream.
        /// </summary>
        /// <returns>16 bit float.</returns>
        public Half ReadHalf() {
            this.ReadBytesWithEndian(2);
            return DataUtils.ConvertByteArrayToHalf(this._buffer);
        }
        
        /// <inheritdoc cref="BinaryReader.ReadSingle"/>
        public override float ReadSingle() {
            this.ReadBytesWithEndian(4);
            return DataUtils.ConvertByteArrayToFloat(this._buffer);
        }
        
        /// <inheritdoc cref="BinaryReader.ReadDouble"/>
        public override double ReadDouble() {
            this.ReadBytesWithEndian(8);
            return DataUtils.ConvertByteArrayToDouble(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadDecimal"/>
        public override decimal ReadDecimal() {
            this.ReadBytesWithEndian(16);
            return DataUtils.ConvertByteArrayToDecimal(this._buffer);
        }
        
        /// <summary>
        /// Reads a signed 8-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint8</returns>
        public FixedPoint8 ReadFixed8(int numberOfDecimalBits) {
            return new FixedPoint8(this.ReadSByte(), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads an unsigned 8-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint8</returns>
        public UFixedPoint8 ReadUFixed8(int numberOfDecimalBits) {
            return new UFixedPoint8(this.ReadByte(), numberOfDecimalBits);
        }
        
        /// <summary>
        /// Reads a signed 16-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint16</returns>
        public FixedPoint16 ReadFixed16(int numberOfDecimalBits) {
            return new FixedPoint16(this.ReadUInt16(), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads an unsigned 16-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint16</returns>
        public UFixedPoint16 ReadUFixed16(int numberOfDecimalBits) {
            return new UFixedPoint16(this.ReadUInt16(), numberOfDecimalBits);
        }
        
        /// <summary>
        /// Reads a signed 32-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint32</returns>
        public FixedPoint32 ReadFixed32(int numberOfDecimalBits) {
            return new FixedPoint32(this.ReadInt32(), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads an unsigned 32-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint32</returns>
        public UFixedPoint32 ReadUFixed32(int numberOfDecimalBits) {
            return new UFixedPoint32(this.ReadUInt32(), numberOfDecimalBits);
        }

        /// <summary>
        /// Just a renamed call to ReadHalf().
        /// </summary>
        /// <returns>16 bit float.</returns>
        public Half ReadFloat16() {
            return this.ReadHalf();
        }

        /// <summary>
        /// Just a renamed call to ReadSingle().
        /// </summary>
        /// <returns>readFloat32</returns>
        public float ReadFloat32() {
            return this.ReadSingle();
        }

        /// <inheritdoc cref="BinaryReader.ReadInt32"/>
        public override short ReadInt16() {
            this.ReadBytesWithEndian(2);
            return DataUtils.ConvertByteArrayToShort(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadUInt16"/>
        public override ushort ReadUInt16() {
            this.ReadBytesWithEndian(2);
            return DataUtils.ConvertByteArrayToUShort(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadInt32"/>
        public override int ReadInt32() {
            this.ReadBytesWithEndian(4);
            return DataUtils.ConvertByteArrayToInt(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadUInt32"/>
        public override uint ReadUInt32() {
            this.ReadBytesWithEndian(4);
            return DataUtils.ConvertByteArrayToUInt(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadInt64"/>
        public override long ReadInt64() {
            this.ReadBytesWithEndian(8);
            return DataUtils.ConvertByteArrayToLong(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.ReadUInt64"/>
        public override ulong ReadUInt64() {
            this.ReadBytesWithEndian(8);
            return DataUtils.ConvertByteArrayToULong(this._buffer);
        }

        /// <summary>
        /// Reads an enum value from the reader.
        /// </summary>
        /// <typeparam name="TPrimitive">The primitive type which should be read.</typeparam>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>readEnum</returns>
        /// <exception cref="InvalidOperationException">Thrown if the read value is not a valid enum type.</exception>
        [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
        public TEnum ReadEnum<TPrimitive, TEnum>() where TEnum : Enum {
            TypeCode typeCode = Type.GetTypeCode(typeof(TPrimitive));
            TPrimitive value = typeCode switch {
                TypeCode.Byte => (TPrimitive)(object)this.ReadByte(),
                TypeCode.SByte => (TPrimitive)(object)this.ReadSByte(),
                TypeCode.Char => (TPrimitive)(object)this.ReadChar(),
                TypeCode.Int16 => (TPrimitive)(object)this.ReadInt16(),
                TypeCode.UInt16 => (TPrimitive)(object)this.ReadUInt16(),
                TypeCode.Int32 => (TPrimitive)(object)this.ReadInt32(),
                TypeCode.UInt32 => (TPrimitive)(object)this.ReadUInt32(),
                TypeCode.Int64 => (TPrimitive)(object)this.ReadInt64(),
                TypeCode.UInt64 => (TPrimitive)(object)this.ReadUInt64(),
                _ => throw new InvalidOperationException($"Cannot read enum value from primitive type {typeof(TPrimitive).GetDisplayName()}. Type Code: {typeCode.CalculateName()}")
            };

            TEnum castedValue = (TEnum)(object)value;
            if (!Enum.IsDefined(typeof(TEnum), castedValue))
                throw new InvalidOperationException($"Enum {typeof(TEnum).GetDisplayName()} does not have a definition for value '{value}'.");
            return castedValue;
        }
        
        /// <summary>
        /// Reads an bit flag enum value from the reader.
        /// </summary>
        /// <typeparam name="TPrimitive">The primitive type which should be read.</typeparam>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>readEnumValue</returns>
        [SuppressMessage("ReSharper", "SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault")]
        public TEnum ReadBitFlagEnum<TPrimitive, TEnum>() where TEnum : Enum {
            TypeCode typeCode = Type.GetTypeCode(typeof(TPrimitive));
            return (TEnum)(object) (typeCode switch {
                TypeCode.Byte => this.ReadByte(),
                TypeCode.SByte => this.ReadSByte(),
                TypeCode.Char => this.ReadChar(),
                TypeCode.Int16 => this.ReadInt16(),
                TypeCode.UInt16 => this.ReadUInt16(),
                TypeCode.Int32 => this.ReadInt32(),
                TypeCode.UInt32 => this.ReadUInt32(),
                TypeCode.Int64 => this.ReadInt64(),
                TypeCode.UInt64 => this.ReadUInt64(),
                _ => throw new InvalidOperationException($"Cannot read enum value from primitive type {typeof(TPrimitive).GetDisplayName()}. TypeCode: {typeCode.CalculateName()}")
            });
        }

        /// <summary>
        /// Reads a signed 16-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint16</returns>
        public FixedPoint16 ReadFixed16(int numberOfDecimalBits, ByteEndian endian) {
            return new FixedPoint16(this.ReadUInt16(endian), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads an unsigned 16-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint16</returns>
        public UFixedPoint16 ReadUFixed16(int numberOfDecimalBits, ByteEndian endian) {
            return new UFixedPoint16(this.ReadUInt16(endian), numberOfDecimalBits);
        }
        
        /// <summary>
        /// Reads a signed 32-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint32</returns>
        public FixedPoint32 ReadFixed32(int numberOfDecimalBits, ByteEndian endian) {
            return new FixedPoint32(this.ReadInt32(endian), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads an unsigned 32-bit fixed point number.
        /// </summary>
        /// <returns>fixedPoint32</returns>
        public UFixedPoint32 ReadUFixed32(int numberOfDecimalBits, ByteEndian endian) {
            return new UFixedPoint32(this.ReadUInt32(endian), numberOfDecimalBits);
        }

        /// <summary>
        /// Reads a signed 16-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public short ReadInt16(ByteEndian endian) {
            this.ReadBytesWithEndian(2, endian);
            return DataUtils.ConvertByteArrayToShort(this._buffer);
        }

        /// <summary>
        /// Reads an unsigned 16-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public ushort ReadUInt16(ByteEndian endian) {
            this.ReadBytesWithEndian(2, endian);
            return DataUtils.ConvertByteArrayToUShort(this._buffer);
        }

        /// <summary>
        /// Reads a signed 32-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public int ReadInt32(ByteEndian endian) {
            this.ReadBytesWithEndian(4, endian);
            return DataUtils.ConvertByteArrayToInt(this._buffer);
        }

        /// <summary>
        /// Reads an unsigned 32-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public uint ReadUInt32(ByteEndian endian) {
            this.ReadBytesWithEndian(4, endian);
            return DataUtils.ConvertByteArrayToUInt(this._buffer);
        }

        /// <summary>
        /// Reads a signed 64-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public long ReadInt64(ByteEndian endian) {
            this.ReadBytesWithEndian(8, endian);
            return DataUtils.ConvertByteArrayToLong(this._buffer);
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer with the given byte endian.
        /// </summary>
        /// <param name="endian">The endian to read the value with.</param>
        /// <returns>readValue</returns>
        public ulong ReadUInt64(ByteEndian endian) {
            this.ReadBytesWithEndian(8, endian);
            return DataUtils.ConvertByteArrayToULong(this._buffer);
        }

        /// <inheritdoc cref="BinaryReader.Dispose"/>
        protected override void Dispose(bool dispose) {
            base.Dispose(dispose);
            this._buffer = null;
            this._jumpStack = null;
        }
    }
}
