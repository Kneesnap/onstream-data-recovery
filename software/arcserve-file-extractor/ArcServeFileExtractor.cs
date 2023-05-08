using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using ModToolFramework.Utils.DataStructures;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Uniquely identifies a 32Kb block of data from a tape dump.
    /// </summary>
    public class OnStreamTapeBlock
    {
        public readonly TapeDumpFile File;
        public readonly long Index; // Index of the block within the file it was found in.
        public readonly uint PhysicalBlock;
        public uint LogicalBlock => OnStreamPhysicalPosition.ConvertPhysicalBlockToLogical(this.PhysicalBlock);
        public string LogicalBlockString => OnStreamPhysicalPosition.ConvertPhysicalBlockToLogicalString(this.PhysicalBlock);

        public OnStreamTapeBlock(TapeDumpFile file, long index, uint physicalBlock) {
            this.File = file;
            this.Index = index;
            this.PhysicalBlock = physicalBlock;
        }
    }

    internal class FileError
    {
        /// <summary>
        /// The physical position which the last file started at.
        /// </summary>
        public readonly OnStreamTapeBlock LastFileStartingBlock;

        /// <summary>
        /// The last file seen before the error.
        /// </summary>
        public readonly string LastFileName;

        /// <summary>
        /// The first "end of file" marker seen after the error.
        /// </summary>
        public string? NextEofFileName;
        
        /// <summary>
        /// The block which the next EOF is in.
        /// </summary>
        public OnStreamTapeBlock? NextEofStartingBlock;
        
        /// <summary>
        /// The first file definition after the error.
        /// </summary>
        public string? NextFileName;
        
        /// <summary>
        /// The block which the next EOF is in.
        /// </summary>
        public OnStreamTapeBlock? NextFileStartingBlock;

        public FileError(string lastFileName, OnStreamTapeBlock lastFileStartAtBlock) {
            this.LastFileName = lastFileName;
            this.LastFileStartingBlock = lastFileStartAtBlock;
        }

        /// <summary>
        /// Writes information about this error to the log.
        /// </summary>
        /// <param name="logger">The logger to write data to.</param>
        public void WriteToLog(ILogger logger) {
            logger.LogInformation("Error:");
            logger.LogInformation($" - First Damaged File: '{this.LastFileName}' ({this.LastFileStartingBlock.LogicalBlockString}/{this.LastFileStartingBlock.PhysicalBlock:X8})");
            if (this.NextEofFileName != null) 
                logger.LogInformation($" - Last Damaged File: '{this.NextEofFileName}' ({this.NextEofStartingBlock?.LogicalBlockString}/{this.NextEofStartingBlock?.PhysicalBlock:X8})");
            if (this.NextFileName != null) 
                logger.LogInformation($" - First Undamaged File: '{this.NextFileName}' ({this.NextFileStartingBlock?.LogicalBlockString}/{this.NextFileStartingBlock?.PhysicalBlock:X8})");
            logger.LogInformation(string.Empty);
        }
    }

    internal class TapeDumpData
    {
        public readonly List<FileError> Errors = new List<FileError>();
        public readonly TapeConfig Config;
        public readonly ZipArchive Archive;
        public string? CurrentBasePath;

        public TapeDumpData(TapeConfig config, ZipArchive archive) {
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
        public static void ExtractFilesFromTapeDumps(TapeConfig tape) {
            // Setup logger.
            string logFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + " Extraction.log");
            using FileLogger logger = new FileLogger(logFilePath, true);

            ExtractFilesFromTapeDumps(tape, logger);
        }

        private static Dictionary<uint, OnStreamTapeBlock> GenerateBlockMapping(TapeConfig tape, ILogger logger) {
            Dictionary<uint, OnStreamTapeBlock> blockMap = new Dictionary<uint, OnStreamTapeBlock>();

            logger.LogInformation("Scanning tape chunks to map out their contents...");
            foreach (TapeDumpFile entry in tape.Entries) {
                logger.LogInformation($"Scanning '{entry.GetFileName("dump")}'...");

                uint logicalPosition = (uint)(entry.BlockIndex ?? 0);
                using DataReader reader = new DataReader(entry.RawStream, true);
                reader.JumpTemp(0);
                while (reader.HasMore) {
                    // If there's an error, skip the logical position.
                    while (entry.Errors.Contains(logicalPosition))
                        logicalPosition++;
                    
                    // Read data.
                    long fileIndex = reader.Index;
                    long fileIndexWithoutAux = OnStreamDataStream.RemoveAuxSectionsFromIndex(fileIndex);
                    reader.Index += OnStreamDataStream.DataSectionSize + (1 * DataConstants.IntegerSize);
                    if (!reader.HasMore)
                        continue; // For some reason we have a bad frogger dump that wants this.

                    uint marker = reader.ReadUInt32(ByteEndian.BigEndian);
                    uint physicalPosition = reader.ReadUInt32(ByteEndian.BigEndian);
                    reader.Index += OnStreamDataStream.AuxSectionSize - (3 * DataConstants.IntegerSize);

                    if (marker == 0x57545354) { // 'WTST' aka "Write Stop" -> ArcServe has this data after the end as a marker that the data is done.
                        // If we have any of this, we can safely ignore it.
                        logicalPosition++;
                        continue;
                    }

                    // Determine position:
                    if (physicalPosition != 0 && physicalPosition != 0xFFFFFFFFU) { // This happened with one of the frogger 2 dumps.
                        // Do nothing, we're good.
                    } else if (entry.HasBlockIndex) {
                        logger.LogWarning($" - {reader.GetFileIndexDisplay(fileIndex)} in {entry.FileName} reported an invalid physical position. We've calculated it to be {logicalPosition} instead.");
                        physicalPosition = OnStreamPhysicalPosition.ConvertLogicalBlockToPhysical(logicalPosition);
                    } else {
                        logger.LogWarning($" - {reader.GetFileIndexDisplay(fileIndex)} in {entry.FileName} reported an invalid physical position. We could not calculate it because this is a parking zone.");
                        logicalPosition++;
                        continue;
                    }

                    // Track the block.
                    blockMap[physicalPosition] = new OnStreamTapeBlock(entry, fileIndexWithoutAux, physicalPosition);
                    logicalPosition++;
                }

                reader.JumpReturn();
            }

            logger.LogInformation($"Scan complete, mapped {blockMap.Count} blocks.");
            return blockMap;
        }

        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        /// <param name="logger">The logger to write output to.</param>
        public static void ExtractFilesFromTapeDumps(TapeConfig tape, ILogger logger) {
            // Generate mapping:
            Dictionary<uint, OnStreamTapeBlock> blockMapping = GenerateBlockMapping(tape, logger);
            FindAndLogGaps(blockMapping, logger);
            
            // Show the tape image.
            Image image = TapeImageCreator.CreateImage(blockMapping);
            image.Save(Path.Combine(tape.FolderPath, "tape-damage.png"), ImageFormat.Png);

            // Setup zip file.
            string zipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);
            using ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create, Encoding.UTF8);

            // Setup reader
            using DataReader reader = new DataReader(new OnStreamInterwovenStream(tape, blockMapping));
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

                reader.Align(ArcServe.RootSectorSize);
            }

            logger.LogInformation("Finished reading tape dumps...");
            archive.Dispose();
            logger.LogInformation("Finished cleanup.");
            
            // Display report on files.
            if (tapeData.Errors.Count > 0) {
                logger.LogInformation(string.Empty);
                logger.LogInformation($"{tapeData.Errors.Count} errors were found!!");
                logger.LogInformation("Errors generally occur due to incomplete data dumps, damaged data dumps, not dumping certain parts of the tape.");
                logger.LogInformation(string.Empty);
                tapeData.Errors.ForEach(error => error.WriteToLog(logger));
            }
            
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

                if (dumpData.Errors.Count > 0 && dumpData.Errors[^1].NextEofFileName == null) {
                    dumpData.Errors[^1].NextEofFileName = dosPath;
                    dumpData.Errors[^1].NextEofStartingBlock = reader.GetCurrentTapeBlock(); 
                }

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

            if (data.Errors.Count > 0 && data.Errors[^1].NextFileName == null) {
                data.Errors[^1].NextFileName = fullFilePath;
                data.Errors[^1].NextFileStartingBlock = currentTapeBlock;
            }

            // Log info.
            logger.LogInformation($"Found: {fullFilePath}, {DataUtils.ConvertByteCountToFileSize(fileDefinition.FileSizeInBytes)}"
                + $" @ {reader.GetFileIndexDisplay()}, Block: {currentTapeBlock.LogicalBlockString}/{currentTapeBlock.PhysicalBlock:X8}"
                + $" | Creation: {fileDefinition.FileCreationTime}, Last Modification: {fileDefinition.LastModificationTime}");
            
            if (ArcServe.FastDebuggingEnabled)
                return true;

            try {
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
                        currentTapeBlock = reader.GetCurrentTapeBlock();
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
            } catch (Exception) {
                data.Errors.Add(new FileError(fullFilePath, currentTapeBlock));
                throw;
            }

            return true;
        }

        /// <summary>
        /// Find and log gaps in the tape data layout.
        /// </summary>
        /// <param name="blockMapping">The mapping of blocks.</param>
        /// <param name="logger">The logger to log with.</param>
        private static void FindAndLogGaps(Dictionary<uint, OnStreamTapeBlock> blockMapping, ILogger logger) {
            BitArray array = new BitArray(OnStreamPhysicalPosition.FramesPerTrack * OnStreamPhysicalPosition.TrackCount);
            foreach (uint physicalPosition in blockMapping.Keys) {
                OnStreamPhysicalPosition.FromPhysicalBlock(physicalPosition, out OnStreamPhysicalPosition tempPos);
                int tempValue = (tempPos.Track * OnStreamPhysicalPosition.FramesPerTrack) + tempPos.X;
                array.Set(tempValue, true);
            }

            OnStreamPhysicalPosition.FromLogicalBlock(0, out OnStreamPhysicalPosition currentPos); // Tape origin. Includes the parking zone.
            List<TapeEmptyGap> gaps = new List<TapeEmptyGap>();
            
            int gapSize = 0;
            OnStreamPhysicalPosition firstOpenPhysPos = currentPos;
            do {
                int arrayIndex = (currentPos.Track * OnStreamPhysicalPosition.FramesPerTrack) + currentPos.X;
                if (!array.Get(arrayIndex)) {
                    if (gapSize++ == 0)
                        firstOpenPhysPos = currentPos;
                    continue;
                }

                if (gapSize > 0) {
                    gaps.Add(new TapeEmptyGap(in firstOpenPhysPos, in currentPos, gapSize));
                    gapSize = 0;
                }
            } while (ArcServe.TryIncrementBlockIncludeParkingZone(in currentPos, out currentPos));
            
            // Sort first by X position, then by track.
            // The purpose of this is to organize data located physically together, so when using this as a list of data to dump,
            //  it will organize the data together to reduce the amount of seeking required
            // And also when I dump data based on this list it's nice to have this data together because it
            //  allows me to avoid seeking constantly by organizing all the data in a certain area
            gaps.Sort(Comparer<TapeEmptyGap>.Create((a, b) => {
                var res= a.StartPos.X.CompareTo(b.StartPos.X);
                return res != 0 ? res : a.StartPos.Track.CompareTo(b.StartPos.Track);
            }));
            
            if (gapSize > 0) // Add this after the sorting, so this shows up last.
                gaps.Add(new TapeEmptyGap(in firstOpenPhysPos, in currentPos, gapSize));

            if (gaps.Count > 0) {
                logger.LogInformation("Missing Data Ranges (No data is here, it could be damaged, or maybe there's just no data written there):");

                // Display the gaps.
                foreach (TapeEmptyGap gap in gaps) 
                    logger.LogInformation($" - Gap Position: {gap.StartPos.ToPhysicalBlock():X8} -> {gap.EndPos.ToPhysicalBlock():X8}, Logical Blocks: {gap.StartPos.ToLogicalBlockString()} -> {gap.EndPos.ToLogicalBlockString()}, Gap Size (Blocks): {gap.BlockCount}");

                logger.LogInformation("");
            }
        }
        
        /// <summary>
        /// Represents a gap in the data which was provided to the program.
        /// </summary>
        private class TapeEmptyGap
        {
            public readonly OnStreamPhysicalPosition StartPos;
            public readonly OnStreamPhysicalPosition EndPos;
            public readonly int BlockCount;
            
            public TapeEmptyGap(in OnStreamPhysicalPosition startPos, in OnStreamPhysicalPosition endPos, int blockCount) {
                this.StartPos = startPos;
                this.EndPos = endPos;
                this.BlockCount = blockCount;
            }
        }
    }
}