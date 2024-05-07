using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Text;
using OnStreamSCArcServeExtractor.Packets;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Represents an ArcServe catalog file (.CAT), written at the end of a session.
    /// </summary>
    public class ArcServeCatalogueFile
    {
        public readonly List<ArcServeCatalogueFileEntry> Entries = new ();
        public readonly ArcServeSessionHeader SessionHeader;
        public string? Name;
        public uint TapeNumber;
        public uint TapeId; // The ID which shows in ArcServe, like ID - 17BE.
        public uint SessionNumber;

        public ArcServeCatalogueFile(ArcServeSessionHeader sessionHeader)
        {
            this.SessionHeader = sessionHeader;
        }

        /// <summary>
        /// Loads the catalogue file data from the reader.
        /// </summary>
        /// <param name="tapeArchive">The tape archive to read the catalogue file for</param>
        /// <param name="reader">The reader to read from.</param>
        public static ArcServeCatalogueFile Read(ArcServeTapeArchive tapeArchive, DataReader reader) {
            ArcServeSessionHeader header = ArcServeSessionHeader.ReadSessionHeader(tapeArchive, reader);
            ArcServeCatalogueFile newFile = new (header);

            newFile.Name = reader.ReadFixedSizeString(24);
            newFile.TapeNumber = reader.ReadUInt32();
            newFile.TapeId = reader.ReadUInt32();
            newFile.SessionNumber = reader.ReadUInt32();

            while (reader.HasMore) {
                ArcServeCatalogueFileEntry? fileEntry = ArcServeCatalogueFileEntry.TryReadFileEntry(reader);
                if (fileEntry != null)
                    newFile.Entries.Add(fileEntry);
            }

            return newFile;
        }

        /// <summary>
        /// Find missing files in the zip file created by the tape config.
        /// </summary>
        /// <param name="tapeArchive">The loaded tape archive to find missing files from.</param>
        public static void FindMissingFilesFromTapeZip(ArcServeTapeArchive tapeArchive)
        {
            using ZipArchive? archive = ArcServeFileExtractor.OpenExtractedZipArchive(tapeArchive.Definition, tapeArchive.Logger);
            if (archive != null)
                FindMissingFilesFromZipFile(tapeArchive, archive, tapeArchive.Logger);
        }
        
        /// <summary>
        /// Finds and logs missing/damaged files from a zip file.
        /// </summary>
        /// <param name="tapeArchive">The tape archive to get data from.</param>
        /// <param name="zipArchive">The zip archive to read from.</param>
        /// <param name="logger">The logger to write to.</param>
        public static void FindMissingFilesFromZipFile(ArcServeTapeArchive tapeArchive, ZipArchive zipArchive, ILogger logger) {
            foreach (ZipArchiveEntry entry in zipArchive.Entries) {
                if (!entry.Name.EndsWith(".CAT", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                using Stream zipStream = entry.Open();
                using MemoryStream memoryStream = new ();
                zipStream.CopyTo(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
                using DataReader reader = new (memoryStream);
                ArcServeCatalogueFile catalogFile = Read(tapeArchive, reader);

                logger.LogInformation(string.Empty);
                logger.LogInformation("Finding missing/damaged files in catalog entry '{zipEntryName}/{entryName}':", entry.Name, catalogFile.Name);
                FindMissingFilesFromCatFile(zipArchive, catalogFile, logger);
                logger.LogInformation(string.Empty);
            }
        }

        /// <summary>
        /// Prints catalog information to the logger.
        /// </summary>
        /// <param name="file">The catalog file to write information form.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="reader">The reader to calculate display indices with.</param>
        public static void PrintCatalogInfo(ArcServeCatalogueFile file, ILogger logger, DataReader? reader) {
            logger.LogInformation("Catalogue '{entryName}' [Tape#: {tapeNumber}, Tape ID: {tapeId:X4}, Session#: {sessionNumber}]:", file.Name, file.TapeNumber, file.TapeId, file.SessionNumber);
            
            // Write session header.
            file.SessionHeader.WriteInformation(reader);
            
            // Write catalog entries.
            StringBuilder builder = new ();
            logger.LogInformation("Entries:");
            foreach (ArcServeCatalogueFileEntry fileEntry in file.Entries) {
                builder.Append(' ');
                fileEntry.PrintInformation(builder, reader);
                logger.LogInformation("{message}", builder.ToString());
                builder.Clear();
            }
            
            logger.LogInformation(string.Empty);
        }
        
        private static void FindMissingFilesFromCatFile(ZipArchive archive, ArcServeCatalogueFile file, ILogger logger) {
            string lastPath = file.SessionHeader.RootDirectoryPath;

            long matchesFound = 0;
            long errorsFound = 0;
            long fileSizeFromCatalog = 0;
            long fileSizeFromZip = 0;
            long fileSizeFromZipWithDamage = 0;
            foreach (ArcServeCatalogueFileEntry fileEntry in file.Entries) {
                string folderPath = fileEntry.FolderPath;
                if (string.IsNullOrEmpty(folderPath))
                    folderPath = lastPath;
                if (!folderPath.EndsWith("\\", StringComparison.InvariantCulture))
                    folderPath += "\\";

                // Remove anything before a drive index. (Eg '\\SERVER\D:\Frogger' should be 'D:\Frogger')
                int lastColon = folderPath.LastIndexOf(':');
                if (lastColon >= 1)
                    folderPath = folderPath[(lastColon - 1)..];

                string windowsSearchPath = (folderPath + fileEntry.FileName).Replace('/', '\\');
                if (fileEntry.IsDirectory && !windowsSearchPath.EndsWith("\\", StringComparison.InvariantCulture))
                    windowsSearchPath += "\\";
                string unixSearchPath = windowsSearchPath.Replace('\\', '/');

                ZipArchiveEntry? zipEntry = archive.GetEntry(windowsSearchPath);
                zipEntry ??= archive.GetEntry(unixSearchPath); 
                if (fileEntry.IsDirectory) {
                    zipEntry ??= archive.GetEntry(windowsSearchPath[..^1]);
                    zipEntry ??= archive.GetEntry(unixSearchPath[..^1]);
                }

                if (zipEntry != null && zipEntry.Length != fileEntry.FileSizeInBytes) {
                    logger.LogInformation(" Damaged: '{zipEntryFullName}', File Length: {catalogFileSize}, Recovered: {zipEntryLength}", zipEntry.FullName, fileEntry.FileSizeInBytes, zipEntry.Length);

                    errorsFound++;
                } else if (zipEntry == null && !(fileEntry.IsDirectory && fileEntry.FileNameLength == 0)) {
                    logger.LogInformation(" Missing: '{folderPath}{fileEntryName}', File Length: {fileEntrySize}", folderPath, fileEntry.FileName, fileEntry.FileSizeInBytes);
                    errorsFound++;
                } else {
                    matchesFound++;
                    fileSizeFromZip += fileEntry.FileSizeInBytes;
                }

                lastPath = folderPath;
                fileSizeFromCatalog += fileEntry.FileSizeInBytes;
                fileSizeFromZipWithDamage += zipEntry?.Length ?? 0;
            }

            logger.LogInformation(string.Empty);
            logger.LogInformation(" {matchesFound} files/folders were successfully recovered.", matchesFound);
            logger.LogInformation(" {errorsFound} catalog error{plural} found.", errorsFound > 0 ? errorsFound : "No", errorsFound == 1 ? " was" : "s were");
            logger.LogInformation(string.Empty);
            logger.LogInformation(" Recovered Data: {formattedSize} ({fileSizeFromZip} bytes)", DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromZip), fileSizeFromZip);
            logger.LogInformation(" Recovered Data With Errors: {formattedSize} ({fileSizeFromZipWithDamage} bytes)", DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromZipWithDamage), fileSizeFromZipWithDamage);
            logger.LogInformation(" Full Session Data: {formattedSize} ({fileSizeFromCatalog} bytes)", DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromCatalog), fileSizeFromCatalog);
            logger.LogInformation(string.Empty);
        }
    }

    public class ArcServeCatalogueFileEntry
    {
        public StreamFileSystem FileSystemType;
        public byte Mode;
        public uint OwnerId;
        public uint Attributes; // This contains the same data seen in the file header entry. For Windows, it's the filesystem attributes seen in WINNT.H.
        public long FileSizeInBytes;
        public DateTime LastModificationTime;
        public uint FileDataPageIndex;
        public uint FileDataPageOffset;
        public byte Flags;
        public string FullFilePath = string.Empty;
        public ushort FileNameLength;

        public int RealFileNameLength => (ushort) (Math.Max(1U, this.FileNameLength) - 1);
        public string FileName => this.FileNameLength > 0 ? this.FullFilePath[^this.RealFileNameLength..] : string.Empty;
        public string FolderPath => this.FileNameLength > 0 ? this.FullFilePath[..^this.RealFileNameLength] : this.FullFilePath;
        public long FileDataRawAddressIndex => ((long)this.FileDataPageIndex * PageSizeInBytes) + this.FileDataPageOffset;
        
        public bool IsFile => ((this.Flags & FlagIsFile) == FlagIsFile);
        public bool IsDirectory => ((this.Flags & FlagIsFile) != FlagIsFile);
        
        public const byte ModePartialPath = 1;
        public const byte ModeFullPath = 2;
        public const byte FlagIsFile = 1;
        public const byte Signature = 0xFF;
        public const int PageSizeInBytes = 0x4000;
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            StringBuilder builder = new();
            this.PrintInformation(builder, null);
            return builder.ToString();
        }
        
        /// <summary>
        /// Writes entry information to the builder.
        /// </summary>
        /// <param name="builder">The builder to write entry information to</param>
        /// <param name="reader">The reader to calculate addresses with.</param>
        public void PrintInformation(StringBuilder builder, DataReader? reader)
        {
            bool isDirectory = this.IsDirectory;
            if (isDirectory) {
                builder.Append("Folder: ");
            } else if (this.IsFile) {
                builder.Append("  File: ");
            } else {
                builder.Append(" Entry: ");
            }
            
            // Write file path:
            builder.Append(this.FullFilePath);

            // File Size:
            if (!isDirectory || this.FileSizeInBytes != 0) {
                builder.Append(", ");
                builder.AppendFormat("{0} ({1} bytes)", DataUtils.ConvertByteCountToFileSize((ulong) this.FileSizeInBytes), this.FileSizeInBytes);
            }

            // Show raw address.
            builder.Append(" @ ");
            builder.Append(reader.GetFileIndexDisplay(this.FileDataRawAddressIndex));
            
            // Dates.
            builder.Append(" | Modified: ");
            builder.Append(this.LastModificationTime);

            // Misc:
            if (this.Mode != 0)
                builder.AppendFormat(", Mode: {0:X}", this.Mode);
            if (this.FileSystemType != StreamFileSystem.Dos)
                builder.AppendFormat(", File System: {0}", this.FileSystemType);
            if (this.Attributes != 0)
                builder.AppendFormat(", Attributes: {0:X}", this.Attributes);
            if (this.OwnerId != 0)
                builder.AppendFormat(", Owner ID: {0}", this.OwnerId);
            if (this.Flags != 0)
                builder.AppendFormat(", Flags: {0:X}", this.Flags);
        }

        /// <summary>
        /// Reads a file entry from the reader.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>readFileEntry</returns>
        public static ArcServeCatalogueFileEntry? TryReadFileEntry(DataReader reader) {
            long entryStartIndex = reader.Index;
            if (reader.ReadByte() != Signature)
                return null;
            
            ArcServeCatalogueFileEntry newEntry = new();
            byte entrySizeInBytes = reader.ReadByte();
            newEntry.FileSystemType = reader.ReadEnum<byte, StreamFileSystem>(); 
            newEntry.Mode = reader.ReadByte();
            newEntry.OwnerId = reader.ReadUInt32(ByteEndian.LittleEndian);
            newEntry.Attributes = reader.ReadUInt32(ByteEndian.LittleEndian);
            uint fileSizeHigh = reader.ReadUInt32(ByteEndian.LittleEndian);
            uint fileSizeLow = reader.ReadUInt32(ByteEndian.LittleEndian);
            newEntry.FileSizeInBytes = ((long)fileSizeHigh << 32) | fileSizeLow;
            newEntry.LastModificationTime = ArcServe.ParseTimeStamp(reader.ReadUInt32(ByteEndian.LittleEndian));
            newEntry.FileDataPageIndex = reader.ReadUInt32();
            newEntry.FileDataPageOffset = reader.ReadUInt32();
            ushort fileNameLength = reader.ReadUInt16(ByteEndian.LittleEndian);
            ushort pathLength = reader.ReadUInt16(ByteEndian.LittleEndian);
            newEntry.Flags = reader.ReadByte();
            reader.SkipBytesRequireEmpty(3);

            // Ensure the size was correct.
            long amountOfBytesRead = reader.Index - entryStartIndex;
            if (amountOfBytesRead != entrySizeInBytes)
                throw new DataException($"The catalog entry at {reader.GetFileIndexDisplay(entryStartIndex)} reported having {entrySizeInBytes} bytes worth of data, but we read {amountOfBytesRead} instead.");

            // Read the file name.
            newEntry.FileNameLength = fileNameLength;
            newEntry.FullFilePath = reader.ReadStringBytes(pathLength - 1);
            byte terminator = reader.ReadByte();
            if (terminator != 0x00)
                throw new DataException($"Expected null terminator byte at 0x{reader.Index:X}, but got {terminator:X2} instead.");
            
            return newEntry;
        }
    }
}