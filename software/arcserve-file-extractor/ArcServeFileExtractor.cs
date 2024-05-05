using Microsoft.Extensions.Logging;
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
using OnStreamSCArcServeExtractor.Packets;

namespace OnStreamSCArcServeExtractor
{
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
            using FileLogger logger = new (logFilePath, true);

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
                using DataReader rawReader = new (tape.Entries[0].RawStream);
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
            using DataReader reader = new (new OnStreamInterwovenStream(tape.CreatePhysicallyOrderedBlockList(blockMapping)));
            ExtractFilesFromTapeDumps(tape, logger, reader);
        }

        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        /// <param name="logger">The logger to write output to.</param>
        /// <param name="reader">The source of tape dump data</param>
        public static ArcServeTapeArchive ExtractFilesFromTapeDumps(TapeDefinition tape, ILogger logger, DataReader reader) {
            // Setup zip file.
            string zipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);
            
            // Creates the output archive.
            using ZipArchive zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create, Encoding.UTF8);
            ArcServeTapeArchive tapeArchive = new (tape, logger, zipArchive);

            // Start reading all the files.
            long invalidPacketCount = 0;
            ArcServeSessionHeader? sessionHeader = default;
            while (reader.HasMore) {
                long preReadIndex = reader.Index;
                uint packetSignature = reader.ReadUInt32();
                
                try {
                    if (!TryReadPacket(tapeArchive, reader, packetSignature, ref invalidPacketCount, ref sessionHeader))
                        invalidPacketCount++;
                } catch (Exception ex) {
                    invalidPacketCount++;
                    logger.LogTrace(ex, "Encountered an error while reading the data for packet {packetSignature:X8} at {currIndex}. (Start: {preReadIndex})", packetSignature, reader.GetFileIndexDisplay(), reader.GetFileIndexDisplay(preReadIndex));
                }

                if (tape.HasOnStreamAuxData && reader.WasMissingDataSkipped(preReadIndex, true, out int blocksSkipped, out OnStreamTapeBlock lastValidBlock)) {
                    logger.LogError(" - Skipped {blocksSkipped} missing tape block(s) from after {lastValidPhysicalBlock:X8}/{lastValidBlock}.", blocksSkipped, lastValidBlock.PhysicalBlock, lastValidBlock.LogicalBlockString);
                }

                reader.Align(ArcServe.RootSectorSize);
            }

            logger.LogInformation("Finished reading tape dumps...");
            zipArchive.Dispose();
            logger.LogInformation("Finished cleanup.");

            // Find missing files.
            using ZipArchive readZipFile = ZipFile.Open(zipFilePath, ZipArchiveMode.Read, Encoding.UTF8);
            ArcServeCatalogueFile.FindMissingFilesFromZipFile(tapeArchive, readZipFile, logger);
            return tapeArchive;
        }

        private static bool TryReadPacket(ArcServeTapeArchive tapeArchive, DataReader reader, uint packetSignature, ref long skipCount, ref ArcServeSessionHeader? sessionHeader) {
            if (packetSignature == 0)
                return true; // We don't load a packet, but we also don't consider this a failure. We're just going to let it skip ahead.
            
            ArcServeFilePacket? newPacket = ArcServeFilePacket.CreateFilePacketFromSignature(tapeArchive, sessionHeader, packetSignature);
            if (newPacket == null)
                return false; // Signature wasn't a recognized packet.
            
            ShowSkippedCount(tapeArchive.Logger, ref skipCount);
            tapeArchive.OrderedPackets.Add(newPacket);
            try {
                newPacket.LoadFromReader(reader); // Load from the reader.
            } catch {
                // Ensure we can see what actually caused the error.
                newPacket.WriteInformation();
                throw;
            }

            // If the packet looks like a valid packet, handle it.
            bool loadSuccess = false;
            if (newPacket.AppearsValid) {
                newPacket.WriteInformation();
                loadSuccess = newPacket.Process(reader);
                if (loadSuccess && newPacket is ArcServeSessionHeader sessionHeaderPacket)
                    sessionHeader = sessionHeaderPacket;
            }

            return loadSuccess;
        }

        private static void ShowSkippedCount(ILogger logger, ref long skipCount) {
            if (skipCount <= 0)
                return;

            logger.LogWarning("Skipped {skipCount} packet section(s) which did not appear to be valid.", skipCount);
            skipCount = 0;
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
            FileStream fileStream = new (zipFilePath, FileMode.Open, FileAccess.Read);
            BufferedStream bufferedStream = new (fileStream);
            return new ZipArchive(bufferedStream, ZipArchiveMode.Read);
        }
    }
}