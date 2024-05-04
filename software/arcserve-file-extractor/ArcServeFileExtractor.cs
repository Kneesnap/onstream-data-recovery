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
            // Raw tapes don't have ordering information. We're relying on the user to have specified the tapes in the correct order.
            if (!tape.HasOnStreamAuxData)
            {
                if (tape.Entries.Count != 1)
                    throw new Exception($"Unexpected number of tape file entries {tape.Entries.Count}");
                    
                // TODO: Create a reader which will read in the desired order.
                using DataReader rawReader = new DataReader(tape.Entries[0].RawStream);
                ExtractFilesFromTapeDumps(tape, logger, rawReader);
                return;
            }
            
            // Generate mapping:
            Dictionary<uint, OnStreamTapeBlock> blockMapping = OnStreamBlockMapping.GetBlockMapping(tape, logger);
            OnStreamGapFinder.FindAndLogGaps(tape.Type, blockMapping, logger);

            // Show the tape image.
            Image image = TapeImageCreator.CreateImage(tape.Type, blockMapping);
            image.Save(Path.Combine(tape.FolderPath, "tape-damage.png"), ImageFormat.Png);

            // Setup reader
            using DataReader reader = new DataReader(new OnStreamInterwovenStream(tape.CreatePhysicallyOrderedBlockList(blockMapping)));
            ExtractFilesFromTapeDumps(tape, logger, reader);
        }

        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        /// <param name="logger">The logger to write output to.</param>
        /// <param name="reader">The source of tape dump data</param>
        public static void ExtractFilesFromTapeDumps(TapeDefinition tape, ILogger logger, DataReader reader) {
            // Setup zip file.
            string zipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);
            using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create, Encoding.UTF8);
            TapeDumpData tapeData = new TapeDumpData(tape, archive);

            // Start reading all the files.
            long invalidSectionCount = 0;
            ArcServeSessionHeader sessionHeader = default;
            while (reader.HasMore) {
                long preReadIndex = reader.Index;
                uint magic = reader.ReadUInt32();
                
                try {
                    if (!TryReadSection(magic, reader, tapeData, logger, ref invalidSectionCount, ref sessionHeader))
                        invalidSectionCount++;
                } catch (Exception ex) {
                    invalidSectionCount++;
                    logger.LogError("{error}", ex.ToString());
                    logger.LogError("Encountered an error while reading the data at {preReadIndex}.", reader.GetFileIndexDisplay(preReadIndex));
                }

                if (tape.HasOnStreamAuxData && reader.WasMissingDataSkipped(preReadIndex, true, out int blocksSkipped, out OnStreamTapeBlock lastValidBlock)) {
                    logger.LogError(" - Skipped {blocksSkipped} missing tape block(s) from after {lastValidPhysicalBlock:X8}/{lastValidBlock}.", blocksSkipped, lastValidBlock.PhysicalBlock, lastValidBlock.LogicalBlockString);
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

        private static bool TryReadSection(uint magic, DataReader reader, TapeDumpData dumpData, ILogger logger, ref long skipCount, ref ArcServeSessionHeader sessionHeader) {
            if (magic == 0x00000000U) {
                // Do nothing, empty sector.
            } else if (Enum.IsDefined(typeof(ArcServeSessionHeaderSignature), magic)) { // Tape header.
                ShowSkippedCount(logger, ref skipCount);
                ArcServeSessionHeaderSignature signature = (ArcServeSessionHeaderSignature) magic;
                ArcServeSessionHeader.ReadSessionHeader(reader, signature, out sessionHeader);
                if (ArcServe.IsValidLookingString(sessionHeader.RootDirectoryPath))
                    dumpData.CurrentBasePath = sessionHeader.RootDirectoryPath;

                logger.LogInformation("");
                sessionHeader.PrintSessionHeaderInformation(logger);
                logger.LogInformation("");
            } else if (magic == 0xCCCCCCCCU) { // File ending.
                ShowSkippedCount(logger, ref skipCount);
                string dosPath = reader.ReadFixedSizeString(246);
                uint crcHash = reader.ReadUInt32();
                reader.SkipBytes(258);
                if (!ArcServe.IsValidLookingString(dosPath) && !(sessionHeader.Signature == ArcServeSessionHeaderSignature.Signature386 && string.IsNullOrEmpty(dosPath)))
                    return false;

                if (crcHash != 0 || !string.IsNullOrEmpty(dosPath)) {
                    logger.LogInformation(" - Reached End of File: {dosPath}, Hash: {crcHash}", dosPath, crcHash);
                } else {
                    logger.LogInformation(" - Reached End of File");
                }

                // TODO: If this is a file (not a directory), check CRC hash matches. (Unless CRC hash is zero..?)
            } else if (magic == 0xABBAABBAU || magic == 0xBBBBBBBBU || magic == 0x55555557U) {
                ShowSkippedCount(logger, ref skipCount);
                return TryReadFileContents(in sessionHeader, reader, dumpData, logger);
            } else {
                return false;
            }

            return true;
        }

        private static void ShowSkippedCount(ILogger logger, ref long skipCount) {
            if (skipCount <= 0)
                return;

            logger.LogWarning("Skipped {sectionCount} section(s) which did not appear to be valid.", skipCount);
            skipCount = 0;
        }

        private static bool TryReadFileContents(in ArcServeSessionHeader header, DataReader reader, TapeDumpData data, ILogger logger) {
            OnStreamTapeBlock? currentTapeBlock = data.Config.HasOnStreamAuxData ? reader.GetCurrentTapeBlock() : null;
            ArcServe.ReadFileEntry(in header, reader, logger, out ArcServeFileDefinition fileDefinition);

            string fullFilePath = fileDefinition.FullPath;
            string? basePath = data.CurrentBasePath;
            if (basePath != null)
                fullFilePath = basePath + (basePath.EndsWith("\\", StringComparison.InvariantCulture) ? string.Empty : "\\") + fullFilePath;

            if (!ArcServe.IsValidLookingString(fullFilePath))
                return false;

            // Log info.
            logger.LogInformation($"Found: {fullFilePath}, {DataUtils.ConvertByteCountToFileSize(fileDefinition.FileSizeInBytes)}"
                + $" @ {reader.GetFileIndexDisplay()}, Block: {currentTapeBlock?.LogicalBlockString}/{currentTapeBlock?.PhysicalBlock:X8}"
                + $" | Creation: {fileDefinition.FileCreationTime}, Last Modification: {fileDefinition.LastModificationTime}");

            if (ArcServe.FastDebuggingEnabled)
                return true;

            if (string.IsNullOrWhiteSpace(fileDefinition.RelativeFilePath))
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
                for (int i = 0; i < (fileDefinition.StreamChunks?.Count ?? 0); i++) {
                    long tempStartIndex = reader.Index;
                    
                    // Get the raw data chunk.
                    ArcServeStreamData streamDataChunk = fileDefinition.StreamChunks[i];
                    if (streamDataChunk is not ArcServeStreamRawData rawData)
                        continue;

                    writtenByteCount += (uint)rawData.UsableData.Length;
                    if (rawData.ExpectedDecompressedSize != 0 && rawData.RawData != rawData.UsableData && rawData.UsableData.Length != rawData.ExpectedDecompressedSize)
                        logger.LogWarning(" - Section {sectionId} (At {tempStartIndex}) was expected to decompress to {rawDataExpectedDecompressedSize} bytes, but actually decompressed to {rawDataUsableLength} bytes.", sectionId, reader.GetFileIndexDisplay(tempStartIndex), rawData.ExpectedDecompressedSize, rawData.UsableData.Length);

                    writer.Write(rawData.UsableData);
                    sectionId++;
                }

                if (writtenByteCount != fileDefinition.FileSizeInBytes)
                    logger.LogError(" - The resulting file is supposed to be {fileSizeInBytes} bytes, but we wrote {writtenByteCount} bytes instead.", fileDefinition.FileSizeInBytes, writtenByteCount);
            } else {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempt to open a zip file which has already been extracted.
        /// </summary>
        /// <param name="tape">The tape definition to load from.</param>
        /// <param name="logger">The logger to log information to.</param>
        /// <returns>The zip archive opened, or null if it was not opened.</returns>
        public static ZipArchive? OpenExtractedZipArchive(TapeDefinition tape, ILogger logger) {
            string zipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (!File.Exists(zipFilePath)) {
                DirectoryInfo? parentDir = Directory.GetParent(tape.FolderPath);

                if (parentDir != null) {
                    string parentFilePath = Path.Combine(parentDir.FullName, tape.DisplayName + ".zip");
                    if (File.Exists(parentFilePath))
                        zipFilePath = parentFilePath;
                }

                if (!File.Exists(zipFilePath)) {
                    logger.LogError($"Expected to find file '{zipFilePath}', but it does not exist!");
                    return null;
                }
            }

            // These resources will be cleared when disposed.
            FileStream fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read);
            BufferedStream bufferedStream = new BufferedStream(fileStream);
            return new ZipArchive(bufferedStream, ZipArchiveMode.Read);
        }
    }
}