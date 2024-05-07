using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using ModToolFramework.Utils.Extensions;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents an ArcServe file header.
    /// </summary>
    public abstract class ArcServeFileHeader : ArcServeFilePacket
    {
        public readonly ArcServeSessionHeader SessionHeader;
        public string SharedRelativeFilePath { get; private set; } = string.Empty; // Relative to the root directory. Seems to be a Dos Path on Windows.
        public string AfpLongName { get; private set; } = string.Empty;
        public byte DirectoryLevel { get; private set; }
        public DateTime SharedLastModificationTime { get; private set; }
        public uint SharedFileSizeInBytes { get; private set; }
        public uint ResourceForkSize { get; private set; }
        public uint Attributes { get; private set; }
        public uint OwnerId { get; private set; }
        public ushort Mask { get; private set; }
        public ArcServeFileClass FileClass { get; private set; }
        public uint TrusteeLength { get; private set; }
        public uint DirectorySpaceRestriction { get; private set; }
        public DateOnly SharedLastAccessDate { get; private set; }
        public DateTime SharedFileCreationTime { get; private set; }
        public List<ArcServeStreamData>? CachedDataChunkStream { get; protected set; }

        // Metadata.
        public string? FormattedReaderStartIndex { get; private set; }
        public OnStreamTapeBlock? CachedStartingTapeBlockPosition { get; private set; }
        
        // Getters:
        public virtual string RelativeFilePath => this.SharedRelativeFilePath; // Relative to the root directory. Seems to be a Dos Path on Windows.
        public virtual DateTime LastModificationTime => this.SharedLastModificationTime;
        public virtual ulong FileSizeInBytes => this.SharedFileSizeInBytes;
        public virtual IFormattable LastAccessDate => this.SharedLastAccessDate;
        public virtual DateTime FileCreationTime => this.SharedFileCreationTime;

        
        public new ArcServeFileHeaderSignature Signature => (ArcServeFileHeaderSignature) base.Signature;
        public virtual bool IsFile => !this.IsDirectory;
        public virtual bool IsDirectory => (this.Attributes & 0x10) == 0x10;
        public override bool AppearsValid => ArcServe.IsValidLookingString(this.SharedRelativeFilePath, true); // The full file path may contain non-ASCII characters, which can't be tested.

        public string FullFilePath
        {
            get { 
                string fullFilePath = this.RelativeFilePath; 
                string? basePath = this.TapeArchive.CurrentBasePath;
                if (basePath != null) 
                    fullFilePath = basePath + (basePath.EndsWith("\\", StringComparison.InvariantCulture) ? string.Empty : "\\") + fullFilePath;

                return fullFilePath;
            }
        }

        public ArcServeFileHeader(ArcServeSessionHeader sessionHeader, ArcServeFileHeaderSignature signature) : base(sessionHeader.TapeArchive, (uint)signature)
        {
            this.SessionHeader = sessionHeader;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            StringBuilder builder = new();
            this.PrintSessionHeaderInformation(builder);
            return builder.ToString();
        }
        
        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader)
        {
            // Store reading metadata.
            this.FormattedReaderStartIndex = reader.GetFileIndexDisplay();
            if (this.TapeArchive.Definition.HasOnStreamAuxData) 
                this.CachedStartingTapeBlockPosition = reader.GetCurrentTapeBlock();
            
            // Start reading.
            this.SharedRelativeFilePath = reader.ReadFixedSizeString(250);
            this.AfpLongName = reader.ReadFixedSizeString(33);
            this.DirectoryLevel = reader.ReadByte();
            this.SharedLastModificationTime = ArcServe.ParseTimeStamp(reader.ReadUInt32(ByteEndian.BigEndian));
            this.SharedFileSizeInBytes = reader.ReadUInt32();
            this.ResourceForkSize = reader.ReadUInt32();
            this.Attributes = reader.ReadUInt32();
            this.OwnerId = reader.ReadUInt32();
            this.Mask = reader.ReadUInt16();
            this.FileClass = reader.ReadEnum<byte, ArcServeFileClass>();
            this.TrusteeLength = reader.ReadUInt32();
            this.DirectorySpaceRestriction = reader.ReadUInt32();
            this.SharedLastAccessDate = ArcServe.ParseDate(reader.ReadUInt16(ByteEndian.LittleEndian));
            this.SharedFileCreationTime = ArcServe.ParseTimeStamp(reader.ReadUInt32(ByteEndian.LittleEndian));
            reader.SkipBytesRequireEmpty(22);
        }

        /// <inheritdoc cref="ArcServeFilePacket.WriteInformation"/>
        public override void WriteInformation(DataReader reader)
        {
            this.Logger.LogInformation("{fileHeaderInfo}", this);
            
            // Write data streak chunk info.
            if (this.CachedDataChunkStream != null) 
                for (int i = 0; i < this.CachedDataChunkStream.Count; i++) 
                    this.CachedDataChunkStream[i].WritePacketReadInfo(this.Logger, reader);
        }
        
        /// <inheritdoc cref="ArcServeFilePacket.Process"/>
        public override bool Process(DataReader reader)
        {
            // Determine full file path.
            long dataStreamStartIndex = reader.Index;
            ArcServeTapeArchive tapeArchive = this.TapeArchive;
            string fullFilePath = this.FullFilePath;

            // TODO: Hmm. I think we can handle this some other way maybe? (Perhaps just if we see an EndOfData marker?)
            if (string.IsNullOrWhiteSpace(this.RelativeFilePath)) {
                this.ReadAndDisplayExtraFileData(reader);
                return true; // It's not a file entry, but instead first file in a session.
            }

            // Handle file.
            if (this.IsDirectory) {
                string folderPath = fullFilePath;
                if (!folderPath.EndsWith("\\", StringComparison.InvariantCulture) && !folderPath.EndsWith("/", StringComparison.InvariantCulture))
                    folderPath += "\\";

                ZipArchiveEntry entry = tapeArchive.Archive.CreateEntry(folderPath, CompressionLevel.Fastest);
                if (this.LastModificationTime != DateTime.UnixEpoch)
                    entry.LastWriteTime = this.LastModificationTime;

                // Show remaining packets.
                this.ReadAndDisplayExtraFileData(reader);
            } else if (this.IsFile) {
                // Create entry for file.
                ZipArchiveEntry entry = tapeArchive.Archive.CreateEntry(fullFilePath, CompressionLevel.Fastest);
                if (this.LastModificationTime != DateTime.UnixEpoch)
                    entry.LastWriteTime = this.LastModificationTime;

                using Stream zipEntry = entry.Open();
                long writerStartPosition = zipEntry.Position;
                this.WriteFileContents(reader, zipEntry);
                long writtenByteCount = zipEntry.Position - writerStartPosition;
                if ((ulong) writtenByteCount != this.FileSizeInBytes && (writtenByteCount != 0 || !ArcServe.FastDebuggingEnabled))
                    this.Logger.LogError(" - Read failure! The file was supposed to be {fileSizeInBytes} bytes, but we wrote {writtenByteCount} bytes instead.", this.FileSizeInBytes, writtenByteCount);
            } else {
                this.Logger.LogError(" - Read failure! The file '{filePath}' was neither a file nor a directory.", fullFilePath);
                return false;
            }
            
            if (!this.IsDirectory && !this.IsFile)
                this.Logger.LogError(" - Expected the type at {sectionReadStart} to either be a File or a Directory, but got ???? instead.", reader.GetFileIndexDisplay(dataStreamStartIndex));

            return true;
        }

        /// <summary>
        /// Called to handle reading extra file data, when feasible.
        /// </summary>
        /// <param name="reader">The reader to read file contents from.</param>
        protected virtual void ReadAndDisplayExtraFileData(DataReader reader) {
            // Do nothing by default.
        }
        
        /// <summary>
        /// Called to handle writing of file contents.
        /// </summary>
        /// <param name="reader">The reader to read file contents from.</param>
        /// <param name="writer">The writer to write file contents to.</param>
        protected abstract void WriteFileContents(DataReader reader, Stream writer);

        /// <summary>
        /// Writes file header information to the builder.
        /// </summary>
        /// <param name="builder">The builder to write file header information to</param>
        public virtual void PrintSessionHeaderInformation(StringBuilder builder)
        {
            bool isDirectory = this.IsDirectory;
            if (isDirectory) {
                builder.Append("Folder");
            } else if (this.IsFile) {
                builder.Append("File");
            } else {
                builder.Append("Entry");
            }
            
            // Write file identification information.
            bool showSignature = (this.Signature != ArcServeFileHeaderSignature.Universal);
            bool showFileClass = (this.FileClass != ArcServeFileClass.NormalFile);
            if (showSignature && showFileClass) {
                builder.AppendFormat("[{0}|{1}]", this.Signature.GetName(), this.FileClass.GetName());
            } else if (showSignature && !showFileClass) {
                builder.AppendFormat("[{0}]", this.Signature.GetName());
            } else if (showFileClass && !showSignature) {
                builder.AppendFormat("[{0}]", this.FileClass.GetName());
            }

            // Write file path:
            builder.Append(": ");
            builder.Append(this.FullFilePath);

            // File Size:
            if (!isDirectory || this.FileSizeInBytes != 0) {
                builder.Append(", ");
                builder.Append(DataUtils.ConvertByteCountToFileSize(this.FileSizeInBytes));
            }

            // Resource Fork Size:
            if (this.ResourceForkSize != 0) {
                builder.Append(", Fork Size: ");
                builder.Append(this.ResourceForkSize);
            }

            // Reader position:
            if (!string.IsNullOrEmpty(this.FormattedReaderStartIndex)) {
                builder.Append(" @ ");
                builder.Append(this.FormattedReaderStartIndex);
            }

            // Write tape block:
            if (this.CachedStartingTapeBlockPosition != null) 
                builder.AppendFormat(", Block: {0}/{1:X8}", this.CachedStartingTapeBlockPosition.LogicalBlockString, this.CachedStartingTapeBlockPosition.PhysicalBlock);
            
            // Dates.
            builder.Append(" | Created: ");
            builder.Append(this.FileCreationTime);
            builder.Append(", Modified: ");
            builder.Append(this.LastModificationTime);
            builder.Append(", Accessed: ");
            builder.Append(this.LastAccessDate);

            // Misc
            if (this.Attributes != 0)
                builder.AppendFormat(", Attributes: {0:X}", this.Attributes);
            if (this.OwnerId != 0)
                builder.AppendFormat(", Owner ID: {0}", this.OwnerId);
            if (this.Mask != 0 && this.Mask != 0xFFFF)
                builder.AppendFormat(", Mask: {0:X4}", this.Mask);
            if (this.DirectoryLevel != 0xFF)
                builder.AppendFormat(", Directory Level: {0:X2}", this.DirectoryLevel);
            if (this.DirectorySpaceRestriction != 0)
                builder.AppendFormat(", Directory Space Restriction: {0}", this.DirectorySpaceRestriction);
            if (this.TrusteeLength != 0)
                builder.AppendFormat(", Trustee Length: {0}", this.TrusteeLength);
        }
    }

    /// <summary>
    /// Known file header signatures.
    /// </summary>
    public enum ArcServeFileHeaderSignature : uint
    {
        Universal = 0xABBAABBAU,
        Dos = 0xBBBBBBBBU,
        Afp = 0xAAAAAAAAU, // Apple Filing Protocol
        Os2 = 0x22222222U,
        Unix = 0x33333333U,
        Mac = 0x44444444U,
        WindowsNt = 0x55555555U,
        WindowsNtWorkstation = 0x55555557U,
        Windows95 = 0x66666666U,
    }
    
    /// <summary>
    /// Known file classes.
    /// </summary>
    public enum ArcServeFileClass : byte
    {
        NormalFile = 0,
        WindowsNtRegistryFile = 1,
        WindowsNtEventLog = 2,
        WindowsNtHardLink1 = 3,
        WindowsNtHardLink2 = 4,
        ArcServeCatalogue = 5,
        EisaConfig = 6,
        DriveRoot = 10,
        SystemFileCatalog = 16,
        SystemFileItem = 17,
        DllCache = 24,
        ExtendedSessionHeader = 32,
        DatabaseFileList = 35,
        Checkpoint = 36,
        Skip = 99,
        SkipFile = 0xFF
    }
}