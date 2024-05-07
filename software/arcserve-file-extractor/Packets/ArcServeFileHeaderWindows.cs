using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Implements a file entry for Windows NT.
    /// Used By: Chicken Run EOP Tape #1, Chicken Run EOP FMV Tape
    /// </summary>
    public class ArcServeFileHeaderWindows : ArcServeFileHeader
    {
        public uint FileAttributes { get; private set; } // Appears to be the same as the parent attributes. These attributes can be found in WINNT.H (Part of Windows), or: https://learn.microsoft.com/en-us/windows/win32/fileio/file-attribute-constants
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
            reader.SkipBytes(85); // I've not yet figured out what this data is. Perhaps it's security filesystem info, or volume info. I see the filesize in here sometimes.
            
            // The actual amount of data seems to vary and sometimes cross the page border.
            // It seems like this data is a different size on a per-tape basis. Eg: All files share the length per-tape.
            // I've not found any data yet which allows determining the size of this data, so for now it can be controlled by a config file.
            // The only tape session data I could find of interest was if the VolumeLevel flag was present. Not sure yet if this is an accurate determiner.
            if (this.TapeArchive.Definition.ShouldArcServeSkipExtraSectionPerFile)
                reader.SkipBytes(ArcServe.RootSectorSize);
        }

        /// <inheritdoc cref="ArcServeFileHeader.PrintSessionHeaderInformation"/>
        public override void PrintSessionHeaderInformation(StringBuilder builder)
        {
            base.PrintSessionHeaderInformation(builder);
            if (this.Attributes != this.FileAttributes)
                builder.AppendFormat(", WinAttributes: {0:X}", this.FileAttributes);
            if (this.Unknown0 != 0)
                builder.AppendFormat(", WinUnknown0: {0:X}", this.Unknown0);
            if (this.Unknown1 != 0)
                builder.AppendFormat(", WinUnknown1: {0:X}", this.Unknown1);
        }
        
        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            if (ArcServe.FastDebuggingEnabled)
            {
                this.Logger.LogDebug(" - File data started at {fileDataStartIndex}", reader.GetFileIndexDisplay());
                reader.SkipBytes((long) this.FileSizeInBytes);
                this.Logger.LogDebug(" - File data ended at {fileDataEndIndex}", reader.GetFileIndexDisplay());
                return;
            }

            _fileReadBuffer ??= new byte[2048];
            this.Logger.LogDebug(" - Starting reading file data at {fileDataStartIndex}", reader.GetFileIndexDisplay());

            // Copy bytes from the reader directly to the writer.
            ulong bytesLeft = this.FileSizeInBytes;
            while (bytesLeft > 0) {
                int bytesRead = reader.Read(_fileReadBuffer, 0, (int) Math.Min((ulong) _fileReadBuffer.LongLength, bytesLeft));
                if (bytesRead <= 0)
                    throw new EndOfStreamException($"There's no more data to read. We read {bytesRead} byte(s), but there are {bytesLeft} byte(s) still expected to be available.");
                
                writer.Write(_fileReadBuffer, 0, bytesRead);
                bytesLeft -= (uint) bytesRead;
            }
            
            this.Logger.LogDebug(" - Stopped reading file data at {fileDataEndIndex}", reader.GetFileIndexDisplay());
        }
    }
}