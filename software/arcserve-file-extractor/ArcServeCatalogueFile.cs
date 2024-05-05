using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using OnStreamSCArcServeExtractor.Packets;

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
        public uint Unknown1;
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
            logger.LogInformation("Finding missing/damaged files...");

            foreach (ZipArchiveEntry entry in zipArchive.Entries) {
                if (!entry.Name.EndsWith(".CAT", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                using Stream zipStream = entry.Open();
                using MemoryStream memoryStream = new ();
                zipStream.CopyTo(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);
                using DataReader reader = new (memoryStream);
                ArcServeCatalogueFile catalogueFile = Read(tapeArchive, reader);
                catalogueFile.SessionHeader.WriteInformation();
                
                logger.LogInformation($"Catalogue '{entry.Name}':");
                FindMissingFilesFromCatFile(zipArchive, catalogueFile, logger);
                logger.LogInformation("");
            }
        }
        
        private static void FindMissingFilesFromCatFile(ZipArchive archive, ArcServeCatalogueFile file, ILogger logger) {
            string lastPath = file.SessionHeader.RootDirectoryPath;

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

                string windowsSearchPath = (folderPath + fileEntry.FileName).Replace('/', '\\');
                if (fileEntry.IsDirectory && !windowsSearchPath.EndsWith("\\", StringComparison.InvariantCulture))
                    windowsSearchPath += "\\";
                string unixSearchPath = windowsSearchPath.Replace('\\', '/');

                ZipArchiveEntry? zipEntry = archive.GetEntry(windowsSearchPath);
                zipEntry ??= archive.GetEntry(unixSearchPath); 
                if (fileEntry.IsDirectory) {
                    zipEntry ??= archive.GetEntry(windowsSearchPath[0..^1]);
                    zipEntry ??= archive.GetEntry(unixSearchPath[0..^1]);
                }

                if (zipEntry != null && zipEntry.Length != fileEntry.FileSize) {
                    logger.LogInformation($" Damaged: '{zipEntry.FullName}', File Length: {fileEntry.FileSize}, Recovered: {zipEntry.Length}");

                    errorsFound++;
                } else if (zipEntry == null) {
                    logger.LogInformation($" Missing: '{folderPath}{fileEntry.FileName}', File Length: {fileEntry.FileSize}");
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