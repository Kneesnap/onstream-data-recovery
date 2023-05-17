using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;
using OnStreamTapeLibrary.Workers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OnStreamSCArcServeExtractor
{
    internal class TapeDumpData
    {
        public readonly TapeDefinition Config;
        public readonly ZipArchive Archive;
        public string? CurrentBasePath;

        public TapeDumpData(TapeDefinition config, ZipArchive archive) {
            this.Config = config;
            this.Archive = archive;
        }
    }

    /// <summary>
    /// This is the meat of the operation, managing the extraction of files from the tape dump.
    /// </summary>
    public static class ArcServeFileExtractor
    {
        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        public static void ExtractFilesFromTapeDumps(TapeDefinition tape) {
            // Setup logger.
            string logFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + " Extraction.log");
            using FileLogger logger = new FileLogger(logFilePath, true);

            ExtractFilesFromTapeDumps(tape, logger);
        }

        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        /// <param name="logger">The logger to write output to.</param>
        public static void ExtractFilesFromTapeDumps(TapeDefinition tape, ILogger logger) {
            // Generate mapping:
            Dictionary<uint, OnStreamTapeBlock> blockMapping = OnStreamBlockMapping.GetBlockMapping(tape, logger);
            OnStreamGapFinder.FindAndLogGaps(tape.Type, blockMapping, logger);

            // Show the tape image.
            Image image = TapeImageCreator.CreateImage(tape.Type, blockMapping);
            image.Save(Path.Combine(tape.FolderPath, "tape-damage.png"), ImageFormat.Png);

            // Setup zip file.
            string zipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);
            using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create, Encoding.UTF8);

            // Setup reader
            using DataReader reader = new DataReader(new OnStreamInterwovenStream(tape.CreatePhysicallyOrderedBlockList(blockMapping)));
            TapeDumpData tapeData = new TapeDumpData(tape, archive);

            // Start reading all the files.
            while (reader.HasMore) {
                long preReadIndex = reader.Index;
                uint magic = reader.ReadUInt32();

                try {
                    _ = TryReadSection(magic, reader, tapeData, logger);
                } catch (Exception ex) {
                    logger.LogError(ex.ToString());
                    logger.LogError($"Encountered an error while reading the data at {reader.GetFileIndexDisplay(preReadIndex)}.");
                }

                if (reader.WasMissingDataSkipped(preReadIndex, true, out int blocksSkipped, out OnStreamTapeBlock lastValidBlock)) {
                    logger.LogError($"Skipped {blocksSkipped} tape missing tape block(s) from after {lastValidBlock.PhysicalBlock:X8}/{lastValidBlock.LogicalBlockString}.");
                }

                reader.Align(ArcServe.RootSectorSize);
            }

            logger.LogInformation("Finished reading tape dumps...");
            archive.Dispose();
            logger.LogInformation("Finished cleanup.");

            // Find missing files.
            using ZipArchive readZipFile = ZipFile.Open(zipFilePath, ZipArchiveMode.Read, Encoding.UTF8);
            ArcServeCatalogueFile.FindMissingFilesFromZipFile(readZipFile, logger);
        }

        private static bool TryReadSection(uint magic, DataReader reader, TapeDumpData dumpData, ILogger logger) {
            if (magic == 0x00000000U) {
                // Do nothing, empty sector.
            } else if (magic == 0xDDDDDDDDU) { // Tape header.
                ArcServe.ReadSessionHeader(reader, out ArcServeSessionHeader header);
                if (ArcServe.IsValidLookingString(header.BasePath)) {
                    // New data.
                    dumpData.CurrentBasePath = header.BasePath;
                    logger.LogInformation("");
                    logger.LogInformation("New Session:");
                    logger.LogInformation($" - Base Path: '{header.BasePath}'");
                    logger.LogInformation($" - Description: '{header.Description}'");
                    logger.LogInformation($" - OS Username: '{header.OsUserName}'");
                    logger.LogInformation($" - Password: '{header.Password}'");
                    logger.LogInformation($" - Flags: {header.Flags:X}");
                    logger.LogInformation("");
                }
            } else if (magic == 0xCCCCCCCCU) { // File ending.
                string dosPath = reader.ReadFixedSizeString(246);
                uint crcHash = reader.ReadUInt32();
                reader.SkipBytes(258);
                if (!ArcServe.IsValidLookingString(dosPath))
                    return false;

                logger.LogInformation($" - Reached End of File: {dosPath}, Hash: {crcHash}");

                // TODO: If this is a file (not a directory), check CRC hash matches. (Unless CRC hash is zero..?)
            } else if (magic == 0xABBAABBAU) {
                return TryReadFileContents(reader, dumpData, logger);
            } else {
                return false;
            }

            return true;
        }

        private static bool TryReadFileContents(DataReader reader, TapeDumpData data, ILogger logger) {
            OnStreamTapeBlock currentTapeBlock = reader.GetCurrentTapeBlock();
            ArcServe.ReadFileEntry(reader, logger, out ArcServeFileDefinition fileDefinition);

            string fullFilePath = fileDefinition.FullPath;
            string? basePath = data.CurrentBasePath;
            if (basePath != null)
                fullFilePath = basePath + (basePath.EndsWith("\\", StringComparison.InvariantCulture) ? string.Empty : "\\") + fullFilePath;

            // Log info.
            logger.LogInformation($"Found: {fullFilePath}, {DataUtils.ConvertByteCountToFileSize(fileDefinition.FileSizeInBytes)}"
                + $" @ {reader.GetFileIndexDisplay()}, Block: {currentTapeBlock.LogicalBlockString}/{currentTapeBlock.PhysicalBlock:X8}"
                + $" | Creation: {fileDefinition.FileCreationTime}, Last Modification: {fileDefinition.LastModificationTime}");

            if (ArcServe.FastDebuggingEnabled)
                return true;

            if (string.IsNullOrWhiteSpace(fileDefinition.DosPath))
                return true; // It's not a file entry, but instead first file in a session.

            // Handle file.
            if (fileDefinition.IsDirectory) {
                string folderPath = fullFilePath;
                if (!folderPath.EndsWith("\\", StringComparison.InvariantCulture) && !folderPath.EndsWith("/", StringComparison.InvariantCulture))
                    folderPath += "\\";

                ZipArchiveEntry entry = data.Archive.CreateEntry(folderPath, CompressionLevel.Fastest);
                if (fileDefinition.LastModificationTime != DateTime.UnixEpoch)
                    entry.LastWriteTime = fileDefinition.LastModificationTime;
            } else if (fileDefinition.IsFile) {
                // Create entry for file.
                ZipArchiveEntry entry = data.Archive.CreateEntry(fullFilePath, CompressionLevel.Fastest);
                if (fileDefinition.LastModificationTime != DateTime.UnixEpoch)
                    entry.LastWriteTime = fileDefinition.LastModificationTime;

                using Stream zipEntry = entry.Open();
                using BufferedStream writer = new BufferedStream(zipEntry);

                uint sectionId = 0;
                long writtenByteCount = 0;
                while (fileDefinition.FileSizeInBytes > writtenByteCount) {
                    long tempStartIndex = reader.Index;
                    ArcServeStreamRawData rawData = ArcServe.RequireSection<ArcServeStreamRawData>(reader, logger);

                    writtenByteCount += (uint)rawData.UsableData.Length;
                    if (rawData.ExpectedDecompressedSize != 0 && rawData.RawData != rawData.UsableData && rawData.UsableData.Length != rawData.ExpectedDecompressedSize)
                        logger.LogWarning($" - Section {sectionId} (At {reader.GetFileIndexDisplay(tempStartIndex)}) was expected to decompress to {rawData.ExpectedDecompressedSize} bytes, but actually decompressed to {rawData.UsableData.Length} bytes.");

                    writer.Write(rawData.UsableData);
                    sectionId++;
                }

                if (writtenByteCount != fileDefinition.FileSizeInBytes)
                    logger.LogError($" - The resulting file is supposed to be {fileDefinition.FileSizeInBytes} bytes, but we only wrote {writtenByteCount} bytes.");

                // Ensure end of data.
                ArcServe.RequireSection<ArcServeStreamEndData>(reader, logger);
            } else {
                return false;
            }

            return true;
        }
    }
}