﻿using System;
using System.IO;
using System.Text;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Implements a file entry for Windows NT.
    /// Used By: Chicken Run EOP Tape #1, Chicken Run EOP FMV Tape
    /// </summary>
    public class ArcServeFileHeaderWindows : ArcServeFileHeader
    {
        public uint FileAttributes { get; private set; }
        public DateTime PreciseCreationTime { get; private set; }
        public DateTime PreciseLastAccessTime { get; private set; }
        public DateTime PreciseLastWriteTime { get; private set; }
        public ulong PreciseFileSizeInBytes { get; private set; }
        public uint Unknown0 { get; private set; }
        public uint Unknown1 { get; private set; }
        public string FullFileName { get; private set; } = string.Empty;
        public string DosFileName { get; private set; } = string.Empty;
        public string FullRelativeFilePath { get; private set; } = string.Empty;

        [ThreadStatic] private static byte[]? _fileReadBuffer;
        
        // Override basic file information.
        public override string RelativeFilePath => string.IsNullOrWhiteSpace(this.FullRelativeFilePath) ? base.RelativeFilePath : this.FullRelativeFilePath;
        public override DateTime LastModificationTime => this.PreciseLastWriteTime;
        public override ulong FileSizeInBytes => this.PreciseFileSizeInBytes;
        public override IFormattable LastAccessDate => this.PreciseLastAccessTime;
        public override DateTime FileCreationTime => this.PreciseCreationTime;

        
        public ArcServeFileHeaderWindows(ArcServeSessionHeader sessionHeader, ArcServeFileHeaderSignature signature) : base(sessionHeader, signature)
        {
        }

        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader)
        {
            base.LoadFromReader(reader);
            this.FileAttributes = reader.ReadUInt32();
            this.PreciseCreationTime = DateTime.FromFileTimeUtc(reader.ReadInt64(ByteEndian.LittleEndian));
            this.PreciseLastAccessTime = DateTime.FromFileTimeUtc(reader.ReadInt64(ByteEndian.LittleEndian));
            this.PreciseLastWriteTime = DateTime.FromFileTimeUtc(reader.ReadInt64(ByteEndian.LittleEndian));
            this.PreciseFileSizeInBytes = (reader.ReadUInt32() << DataConstants.LongSize / 2) | reader.ReadUInt32();
            this.Unknown0 = reader.ReadUInt32();
            this.Unknown1 = reader.ReadUInt32();
            this.FullFileName = reader.ReadFixedSizeString(520, encoding: Encoding.Unicode);
            this.DosFileName = reader.ReadFixedSizeString(28, encoding: Encoding.Unicode);
            this.FullRelativeFilePath = reader.ReadFixedSizeString(1024, encoding: Encoding.Unicode);
            reader.SkipBytes(85); // TODO: Figure out this data.
            reader.SkipBytes(512); // TODO: This needs disabling on one of the tapes. Can we figure out why?
        }

        /// <inheritdoc cref="ArcServeFileHeader.PrintSessionHeaderInformation"/>
        public override void PrintSessionHeaderInformation(StringBuilder builder)
        {
            base.PrintSessionHeaderInformation(builder);
            if (this.Attributes != this.FileAttributes)
                builder.AppendFormat(" WinAttributes: {0:X}", this.FileAttributes);
            if (this.Unknown0 != 0)
                builder.AppendFormat(" WinUnknown0: {0:X}", this.Unknown0);
            if (this.Unknown1 != 0)
                builder.AppendFormat(" WinUnknown1: {0:X}", this.Unknown1);
        }
        
        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            _fileReadBuffer ??= new byte[2048];

            // Copy bytes from the reader directly to the writer.
            ulong bytesLeft = this.FileSizeInBytes;
            while (bytesLeft > 0) {
                int bytesRead = reader.Read(_fileReadBuffer, 0, (int) Math.Min((ulong) _fileReadBuffer.LongLength, bytesLeft));
                if (bytesRead <= 0)
                    throw new EndOfStreamException($"There's no more data to read. We read {bytesRead} byte(s), but there are {bytesLeft} byte(s) still expected to be available.");
                
                writer.Write(_fileReadBuffer, 0, bytesRead);
                bytesLeft -= (uint) bytesRead;
            }
        }
    }
}