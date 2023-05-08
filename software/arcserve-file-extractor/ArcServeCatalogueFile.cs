using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Represents an ArcServe catalog file (.CAT), written at the end of a session.
    /// </summary>
    public class ArcServeCatalogueFile
    {
        public readonly List<ArcServeCatalogueFileEntry> Entries = new List<ArcServeCatalogueFileEntry>();
        public ArcServeSessionHeader SessionHeader;
        public string? Name;
        public uint Unknown1;
        public uint TapeId; // The ID which shows in ArcServe, like ID - 17BE.
        public uint SessionNumber;

        public const uint Signature = 0xDDDDDDDDU;

        /// <summary>
        /// Loads the catalogue file data from the reader.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        public static ArcServeCatalogueFile Read(DataReader reader) {
            _ = reader.ReadUInt32(); // Signature.
            
            ArcServeCatalogueFile newFile = new ArcServeCatalogueFile();
            ArcServe.ReadSessionHeader(reader, out newFile.SessionHeader);
            reader.Align(ArcServe.RootSectorSize);

            newFile.Name = reader.ReadFixedSizeString(24);
            newFile.Unknown1 = reader.ReadUInt32();
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
        /// Finds and logs missing/damaged files from a zip file.
        /// </summary>
        /// <param name="archive">The archive to read from.</param>
        /// <param name="logger">The logger to write to.</param>
        public static void FindMissingFilesFromZipFile(ZipArchive archive, ILogger logger) {
            logger.LogInformation("Finding missing/damaged files...");

            foreach (ZipArchiveEntry entry in archive.Entries) {
                if (!entry.Name.EndsWith(".CAT", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                using Stream zipStream = entry.Open();
                using MemoryStream memoryStream = new MemoryStream();
                zipStream.CopyTo(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
                using DataReader reader = new DataReader(memoryStream);
                ArcServeCatalogueFile catalogueFile = Read(reader);
                
                logger.LogInformation($"Catalogue '{entry.Name}':");
                FindMissingFilesFromCatFile(archive, catalogueFile, logger);
                logger.LogInformation("");
            }
        }
        
        private static void FindMissingFilesFromCatFile(ZipArchive archive, ArcServeCatalogueFile file, ILogger logger) {
            string lastPath = file.SessionHeader.BasePath ?? string.Empty;

            long matchesFound = 0;
            long errorsFound = 0;
            long fileSizeFromCatalogue = 0;
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

                string searchPath = folderPath + fileEntry.FileName;
                if (fileEntry.IsDirectory && !searchPath.EndsWith("\\", StringComparison.InvariantCulture))
                    searchPath += "\\";

                ZipArchiveEntry? zipEntry = archive.GetEntry(searchPath);
                if (zipEntry != null && zipEntry.Length != fileEntry.FileSize) {
                    logger.LogInformation($" Damaged: '{searchPath}', File Length: {fileEntry.FileSize}, Recovered: {zipEntry.Length}");
                    errorsFound++;
                } else if (zipEntry == null) {
                    logger.LogInformation($" Missing: '{searchPath}', File Length: {fileEntry.FileSize}");
                    errorsFound++;
                } else {
                    matchesFound++;
                    fileSizeFromZip += fileEntry.FileSize;
                }

                lastPath = folderPath;
                fileSizeFromCatalogue += fileEntry.FileSize;
                fileSizeFromZipWithDamage += zipEntry?.Length ?? 0;
            }

            logger.LogInformation($" {matchesFound} files/folders were successfully recovered.");
            logger.LogInformation(errorsFound > 0 ? $" {errorsFound} catalogue errors were found." : " None");
            logger.LogInformation(string.Empty);
            logger.LogInformation($" Recovered Data: {DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromZip)} ({fileSizeFromZip} bytes)");
            logger.LogInformation($" Recovered Data With Errors: {DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromZipWithDamage)} ({fileSizeFromZipWithDamage} bytes)");
            logger.LogInformation($" Full Session Data: {DataUtils.ConvertByteCountToFileSize((ulong)fileSizeFromCatalogue)} ({fileSizeFromCatalogue} bytes)");
            logger.LogInformation(string.Empty);
        }
    }

    public class ArcServeCatalogueFileEntry
    {
        public byte Mode;
        public long FileSize;
        public uint LastModificationTimestamp;
        public byte Flags;
        public string FullString = string.Empty;
        public ushort FileNameLength;
        
        public string FileName => this.FullString[^this.FileNameLength..];
        public string FolderPath => this.FullString[..^this.FileNameLength];

        private static readonly byte[] Empty3 = new byte[3];

        public bool IsFile => ((this.Flags & FlagIsFile) == FlagIsFile);
        public bool IsDirectory => ((this.Flags & FlagIsFile) != FlagIsFile);
        
        public const byte ModePartialPath = 1;
        public const byte ModeFullPath = 2;
        public const byte FlagIsFile = 1;

        /// <summary>
        /// Reads a file entry from the reader.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <returns>readFileEntry</returns>
        public static ArcServeCatalogueFileEntry? TryReadFileEntry(DataReader reader) {
            if (reader.ReadByte() != 0xFF)
                return null;

            ArcServeCatalogueFileEntry newEntry = new();
            _ = reader.ReadByte(); // Unknown.
            _ = reader.ReadByte(); // Unknown.
            newEntry.Mode = reader.ReadByte();
            _ = reader.ReadUInt32();
            _ = reader.ReadUInt32();
            uint fileSizeHigh = reader.ReadUInt32(ByteEndian.LittleEndian);
            uint fileSizeLow = reader.ReadUInt32(ByteEndian.LittleEndian);
            newEntry.FileSize = ((long)fileSizeHigh << 32) | fileSizeLow;
            newEntry.LastModificationTimestamp = reader.ReadUInt32(ByteEndian.LittleEndian);
            _ = reader.ReadUInt32();
            _ = reader.ReadUInt32();
            ushort fileNameLength = (ushort) (reader.ReadUInt16() - 1);
            ushort pathLength = reader.ReadUInt16();
            newEntry.Flags = reader.ReadByte();
            reader.VerifyBytes(Empty3);

            newEntry.FileNameLength = fileNameLength;
            newEntry.FullString = reader.ReadStringBytes(pathLength - 1);
            byte terminator = reader.ReadByte();
            if (terminator != 0x00)
                throw new DataException($"Expected null terminator byte at 0x{reader.Index:X}, but got {terminator:X2} instead.");

            return newEntry;
        }
    }
}