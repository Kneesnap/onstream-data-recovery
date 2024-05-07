using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;
using OnStreamTapeLibrary.Workers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RetrospectTape
{
    public static class RetrospectTapeExtractor
    {
        /// <summary>
        /// Extracts files from the tape dumps configured with <see cref="tape"/>.
        /// </summary>
        /// <param name="tape">The configuration for the tape dump file(s).</param>
        public static void ExtractFilesFromTapeDumps(TapeDefinition tape) {
            // Setup logger.
            string logFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".log");
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

            // Setup zip files.
            string mainZipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + ".zip");
            if (File.Exists(mainZipFilePath))
                File.Delete(mainZipFilePath);
            using ZipArchive mainArchive = ZipFile.Open(mainZipFilePath, ZipArchiveMode.Create, Encoding.UTF8);
            
            string snapshotZipFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + " Snapshots.zip");
            if (File.Exists(snapshotZipFilePath))
                File.Delete(snapshotZipFilePath);
            using ZipArchive snapshotArchive = ZipFile.Open(snapshotZipFilePath, ZipArchiveMode.Create, Encoding.UTF8);

            // Setup reader
            List<OnStreamTapeBlock> tapeBlocks = tape.CreateLogicallyOrderedBlockList(blockMapping);
            using DataReader reader = new DataReader(new OnStreamInterwovenStream(tapeBlocks));
            reader.Endian = ByteEndian.BigEndian;

            // Start reading all the files.
            reader.SkipBytes((int)OnStreamDataStream.DataSectionSize); // Skip start.
            ReadTapeDumps(reader, logger, mainArchive, snapshotArchive);

            logger.LogInformation("Finished reading tape dumps...");
            mainArchive.Dispose();
            snapshotArchive.Dispose();
            logger.LogInformation("Finished cleanup.");
            
            OnStreamTapeCondenser.Condense(tape, logger, tapeBlocks);
        }
        
        /// <summary>
        /// Parses an apple date.
        /// Determined by referencing https://developer.apple.com/library/archive/technotes/tn/tn1150.html
        /// </summary>
        /// <param name="appleDate">The apple timestamp to interpret as a date time.</param>
        /// <param name="date">Output storage for the date.</param>
        public static void ParseAppleDate(uint appleDate, out DateTime date) {
            date = new DateTime(1904, 01, 01, 0, 0, 0, DateTimeKind.Utc); // This is the default apple date. UTC is the same as GMT.
            date = date.AddSeconds(appleDate);
        }

        private static bool IsValidZipTimestamp(in DateTime time) {
            return time.Year >= 1980;
        }
        
        private static RetrospectDataStreamChunk? ReadNextChunk(DataReader reader, ILogger logger, ref long startIndex) {
            while (reader.HasMore) {
                startIndex = reader.Index;

                try {
                    if (RetrospectDataStreamChunks.TryParseChunk(reader, logger, out RetrospectDataStreamChunk? chunk) && chunk != null)
                        return chunk;
                } catch (Exception ex) {
                    logger.LogError(ex.ToString());
                    logger.LogError($"Encountered an error while reading the data at {reader.GetFileIndexDisplay(startIndex)}.");
                }

                reader.Index++;
            }

            return null;
        }

        private static void ReadTapeDumps(DataReader reader, ILogger logger, ZipArchive mainArchive, ZipArchive snapshotArchive) {
            RetrospectTapeFileContext context = new (mainArchive, snapshotArchive);
            
            RetrospectDataStreamChunk? lastDefChunk = null;
            while (reader.HasMore) {
                long startIndex = reader.Index;

                RetrospectDataStreamChunk? chunk = ReadNextChunk(reader, logger, ref startIndex);
                if (chunk == null)
                    continue;
                
                // Log chunk data.
                bool skippedData = reader.WasMissingDataSkipped(startIndex, true, out int blocksSkipped);
                if (skippedData || chunk.ShouldLog) {
                    logger.LogInformation($"Found {chunk.GetTypeDisplayName()} at {reader.GetFileIndexDisplay(startIndex)}:");
                    logger.LogInformation($" - {chunk}");
                    if (skippedData)
                        logger.LogError($" - {blocksSkipped} tape block(s) are not present, likely damaging one or more files.");
                }
                
                if (chunk is RetrospectForkChunk forkChunk) {
                    if (context.TrackedChunks.TryGetValue(forkChunk.ResourceId, out var startChunk) && context.UnfinishedFiles.TryGetValue(startChunk, out MemoryStream? stream)) {
                        stream.Write(forkChunk.Data);
                        lastDefChunk = startChunk;
                    } else {
                        logger.LogError($" - Found file fork chunk for {forkChunk.ResourceId:X8} at {reader.GetFileIndexDisplay(startIndex)}, but there was no active file.");
                    }
                } else if (chunk is RetrospectContinueChunk continueChunk) {
                    if (lastDefChunk != null && context.UnfinishedFiles.TryGetValue(lastDefChunk, out MemoryStream? stream)) {
                        stream.Write(continueChunk.Data);
                    } else {
                        logger.LogError($" - Found file continue chunk at {reader.GetFileIndexDisplay(startIndex)}, but there was no active data feed.");
                    }
                } else if (chunk is RetrospectTailChunk tailChunk) {
                    if (context.TrackedChunks.TryGetValue(tailChunk.ResourceId, out var startChunk) && context.UnfinishedFiles.ContainsKey(startChunk)) {
                        context.FinishFile(startChunk, logger, true);
                    } else {
                        logger.LogError($" - Found file tail chunk for {tailChunk.ResourceId:X8} at {reader.GetFileIndexDisplay(startIndex)}, but this could not be completed because it was not started.");
                    }
                    
                    lastDefChunk = null;
                } else if (chunk is RetrospectDirectoryChunk directoryChunk) {
                    lastDefChunk = null;

                    ZipArchiveEntry entry = mainArchive.CreateEntry(context.GetFullPath(directoryChunk, logger), CompressionLevel.Fastest);
                    if (IsValidZipTimestamp(directoryChunk.LastModified)) {
                        entry.LastWriteTime = directoryChunk.LastModified;
                    } else if (IsValidZipTimestamp(directoryChunk.BackupTime)) {
                        entry.LastWriteTime = directoryChunk.BackupTime;
                    }
                } else if (chunk is RetrospectSnapshotChunk snapshotChunk) {
                    if (context.TrackedChunks.TryGetValue(snapshotChunk.RememberId, out var startChunk) && context.UnfinishedFiles.ContainsKey(startChunk))
                        context.FinishFile(startChunk, logger, true);
                    context.UnfinishedFiles[snapshotChunk] = new MemoryStream();
                    lastDefChunk = snapshotChunk;
                } else if (chunk is RetrospectFileChunk fileChunk) {
                    context.UnfinishedFiles[fileChunk] = new MemoryStream();
                    lastDefChunk = fileChunk;
                }

                // Remember chunk.
                if (chunk.ShouldRemember)
                    context.TrackedChunks[chunk.RememberId] = chunk;
            }
            
            // Kill remaining files.
            foreach (RetrospectDataStreamChunk chunk in context.UnfinishedFiles.Keys.ToImmutableList())
                context.FinishFile(chunk, logger, false);
        }


        private class RetrospectTapeFileContext
        {
            private readonly ZipArchive _mainArchive;
            private readonly ZipArchive _snapshotArchive;
            public readonly Dictionary<uint, RetrospectDataStreamChunk> TrackedChunks = new Dictionary<uint, RetrospectDataStreamChunk>();
            public readonly Dictionary<RetrospectDataStreamChunk, MemoryStream> UnfinishedFiles = new Dictionary<RetrospectDataStreamChunk, MemoryStream>();

            public RetrospectTapeFileContext(ZipArchive mainArchive, ZipArchive snapshotArchive) {
                this._mainArchive = mainArchive;
                this._snapshotArchive = snapshotArchive;
            }
            
            public string GetFullPath(RetrospectDataStreamChunk? chunk, ILogger logger) {
                if (chunk is RetrospectSnapshotChunk snapshotChunk)
                    return snapshotChunk.ParentFolderName + "/" + snapshotChunk.FolderName + "/" + snapshotChunk.FinderType + "_" + snapshotChunk.BackupTime.ToFileTimeUtc();

                string filePath = string.Empty;
                while (chunk != null) {
                    uint parentId;
                    if (chunk is RetrospectDirectoryChunk directoryChunk) {
                        filePath = directoryChunk.FolderName + "/" + filePath;
                        parentId = directoryChunk.ParentId;
                    } else if (chunk is RetrospectFileChunk fileChunk && string.IsNullOrEmpty(filePath)) {
                        filePath = fileChunk.FileName;
                        parentId = fileChunk.FolderId;
                    } else {
                        logger.LogError($" - {chunk}");
                        throw new Exception($"When recursively building file path '{filePath}', {chunk.GetTypeDisplayName()} was unexpectedly found as a parent chunk??");
                    }

                    if (!this.TrackedChunks.TryGetValue(parentId, out chunk) && parentId > 1)
                        logger.LogError($" - Failed to find parent ID {parentId:X8} for building file path '{filePath}'.");
                }

                return filePath;
            }
            
            public void FinishFile(RetrospectDataStreamChunk definitionChunk, ILogger logger, bool finishedCorrectly) {
                if (!this.UnfinishedFiles.Remove(definitionChunk, out MemoryStream? stream))
                    return;

                long expectedFileSize;
                DateTime firstTime;
                DateTime secondTime;
                ZipArchive archive;
                if (definitionChunk is RetrospectFileChunk fileChunk) {
                    archive = this._mainArchive;
                    firstTime = fileChunk.LastModified;
                    secondTime = fileChunk.BackupTime;
                    expectedFileSize = fileChunk.FileSize;
                } else if (definitionChunk is RetrospectSnapshotChunk snapshotChunk) {
                    archive = this._snapshotArchive;
                    firstTime = secondTime = snapshotChunk.BackupTime;
                    expectedFileSize = snapshotChunk.FileSize;
                } else {
                    throw new ArgumentOutOfRangeException($"{nameof(definitionChunk)} was of type {definitionChunk.GetTypeDisplayName()}, not a supported type.");
                }
                
                string fullFilePath = this.GetFullPath(definitionChunk, logger);
                ZipArchiveEntry entry = archive.CreateEntry(fullFilePath, CompressionLevel.Fastest);

                // Set time.
                if (IsValidZipTimestamp(firstTime)) {
                    entry.LastWriteTime = firstTime;
                } else if (IsValidZipTimestamp(secondTime)) {
                    entry.LastWriteTime = secondTime;
                }
                
                // Write data.
                using Stream zipStream = entry.Open();
                using BufferedStream bufferedStream = new BufferedStream(zipStream);
                stream.Seek(0, SeekOrigin.Begin);
                long fileLength = stream.Length;
                stream.CopyTo(bufferedStream);
                stream.Dispose();
                
                if (expectedFileSize != fileLength)
                    logger.LogWarning($" - File exported with {fileLength} bytes when it was expected to be {expectedFileSize}. (File: '{fullFilePath}')");
                if (!finishedCorrectly)
                    logger.LogWarning($" - File '{fullFilePath}' was finished without an explicit end-of-file marker.");
            }
        }
    }
}