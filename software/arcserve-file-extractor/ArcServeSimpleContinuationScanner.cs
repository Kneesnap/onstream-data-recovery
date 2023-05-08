using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Scans the ArcServe OnStream tape dump for the order in which files are continued.
    /// </summary>
    public class ArcServeSimpleContinuationScanner
    {
        public static void ReportFileSections(TapeConfig tape) {
            string logFilePath = Path.Combine(tape.FolderPath, tape.DisplayName + " Section Scan.log");
            using FileLogger logger = new FileLogger(logFilePath, true);

            // Start reading all the files.
            Dictionary<string, PartialFileDefinition> fileTracker = new Dictionary<string, PartialFileDefinition>();
            foreach (TapeDumpFile entry in tape.Entries) {
                logger.LogInformation($"Reading contents of '{entry.GetFileName("dump")}'...");

                string? endDosPath = null;
                string? startDosPath = null;
                long lastSection = -1;
                long endSection = -1;
                long startSection = -1;
                using DataReader reader = new DataReader(entry.Stream);
                ArcServeFileDefinition lastSeenFileDefinition = default;

                reader.JumpTemp(0);
                while (true) {
                    long searchStartIndex = reader.Index;
                    long currSection = (reader.Index / PartialFileDefinition.TrackSegmentByteLength);
                    long nextSection = currSection + 1;
                    long stopReadingAt = Math.Min(reader.Size, (nextSection * PartialFileDefinition.TrackSegmentByteLength));

                    if (currSection > lastSection || !reader.HasMore) {
                        if (lastSeenFileDefinition.DosPath != null)
                            logger.LogInformation($" - Last File: '{lastSeenFileDefinition.DosPath}', {DataUtils.ConvertByteCountToFileSize(lastSeenFileDefinition.FileSizeInBytes)}, '{lastSeenFileDefinition.FullPath}'");

                        if (endDosPath != null) {
                            if (fileTracker.TryGetValue(endDosPath, out PartialFileDefinition? file)) {
                                file.ApplyEnding(entry, startSection, startDosPath);
                            } else {
                                fileTracker[endDosPath] = new PartialFileDefinition(entry, endDosPath, startSection, startDosPath);
                            }
                        }

                        if (startDosPath != null) {
                            if (fileTracker.TryGetValue(startDosPath, out PartialFileDefinition? file)) {
                                file.ApplyDefinition(entry, in lastSeenFileDefinition, endSection, endDosPath);
                            } else {
                                fileTracker[startDosPath] = new PartialFileDefinition(entry, in lastSeenFileDefinition, endSection, endDosPath);
                            }
                        }

                        if (!reader.HasMore)
                            break; // Need to run this logic.

                        logger.LogInformation($"Reached Section {currSection}:");
                        lastSeenFileDefinition = default;
                        startDosPath = null;
                        endDosPath = null;
                        endSection = -1;
                        startSection = -1;
                        lastSection = currSection;
                    }

                    uint magic = reader.ReadUInt32();

                    try {
                        if (magic == 0xCCCCCCCCU) {
                            string dosPath = reader.ReadFixedSizeString(246);
                            reader.SkipBytes(262);

                            if (endDosPath == null) {
                                endDosPath = dosPath;
                                endSection = currSection;
                                logger.LogInformation($" - Found end of '{dosPath}'");
                            }
                        } else if (magic == 0xABBAABBAU) {
                            startSection = currSection;

                            ArcServe.ReadFileEntry(reader, logger, out lastSeenFileDefinition);
                            startDosPath = lastSeenFileDefinition.DosPath;

                            if (lastSeenFileDefinition.IsFile) {
                                // Skip file data.

                                while ((stopReadingAt - reader.Index) > ArcServeStreamHeader.SizeInBytes && ArcServe.TryParseStreamHeader(reader, out ArcServeStreamHeader tempBlock)) {
                                    if ((stopReadingAt - reader.Index) < (long)tempBlock.Size) { // Reading this data would put us into the next area.
                                        reader.Index = stopReadingAt;
                                        break;
                                    }

                                    reader.Index += (long)tempBlock.Size;
                                    ArcServe.AlignReaderToStream(reader);
                                }
                            }
                        }
                    } catch (Exception ex) {
                        logger.LogError($" - Error occurred during the search from {reader.GetFileIndexDisplay(searchStartIndex)}.");
                        logger.LogError(ex.ToString());
                    }

                    // Skip empty bytes.
                    long difference = (reader.Index % ArcServe.RootSectorSize);
                    if (difference > 0)
                        reader.SkipBytes((int)(ArcServe.RootSectorSize - difference));
                }

                reader.JumpReturn();
            }

            logger.LogInformation("Finished reading tape dumps.");
            logger.LogInformation("");

            // Build Configuration:
            TapeDumpFile? lastEntry = null;
            long lastSeenSection = -1;
            StringBuilder builder = new StringBuilder("Calculated Order:").Append(Environment.NewLine);
            foreach (PartialFileDefinition fileDefinition in fileTracker.Values) {
                if (fileDefinition.LastFileDosPath != null && fileTracker.ContainsKey(fileDefinition.LastFileDosPath))
                    continue; // Skip ones which have parents.

                PartialFileDefinition? temp = fileDefinition;
                while (temp != null) {
                    if (temp.DefinitionTapeDump != null) {
                        if (lastEntry != temp.DefinitionTapeDump || lastSeenSection != temp.DefinitionSection) {
                            string paddedData = GeneralUtils.PadStringRight($"{temp.DefinitionTapeDump.BlockIndex},{temp.DefinitionSection}", 12, ' ');

                            builder.Append(paddedData)
                                .Append("# ").Append("Finishes: '")
                                .Append(temp.DosPath).Append("', Starts: '")
                                .Append(temp.NextFileDosPath).Append('\'').Append(Environment.NewLine);

                            lastEntry = temp.DefinitionTapeDump;
                            lastSeenSection = temp.DefinitionSection;
                        }
                    } else {
                        builder.Append("# Missing Definition '").Append(Environment.NewLine);
                    }

                    if (temp.EndingTapeDump != null) {
                        if (lastEntry != temp.EndingTapeDump || lastSeenSection != temp.EndingSection) {
                            string paddedData = GeneralUtils.PadStringRight($"{temp.EndingTapeDump.BlockIndex},{temp.EndingSection}", 12, ' ');


                            builder.Append(paddedData)
                                .Append("# ").Append("Finishes: '")
                                .Append(temp.DosPath).Append("', Starts: '")
                                .Append(temp.NextFileDosPath).Append('\'')
                                .Append(Environment.NewLine);

                            lastEntry = temp.EndingTapeDump;
                            lastSeenSection = temp.EndingSection;
                        }
                    } else {
                        builder.Append("# Missing EOF").Append(Environment.NewLine);
                    }

                    temp = temp.NextFileDosPath != null ? fileTracker[temp.NextFileDosPath] : null;
                }

                builder.Append(Environment.NewLine);
            }

            logger.LogInformation(builder.ToString());
        }
    }
    
    public class PartialFileDefinition
    {
        public string DosPath;

        // Definition:
        public ArcServeFileDefinition Definition;
        public TapeDumpFile? DefinitionTapeDump;
        public long DefinitionSection;
        public string? NextFileDosPath;
        public bool HasDefinition => (this.DefinitionTapeDump != null);

        // Ending:
        public string? LastFileDosPath;
        public TapeDumpFile? EndingTapeDump;
        public long EndingSection;
        public bool HasEnding => this.EndingTapeDump != null;

        public const int TrackSegmentLength = 1500;
        public const long TrackSegmentByteLength = (TrackSegmentLength * OnStreamDataStream.DataSectionSize);

        public PartialFileDefinition(TapeDumpFile file, in ArcServeFileDefinition definition, long definitionSection, string? lastFileDosPath) {
            this.DosPath = definition.DosPath;
            this.ApplyDefinition(file, in definition, definitionSection, lastFileDosPath);
        }

        public PartialFileDefinition(TapeDumpFile file, string dosPath, long endingSection, string? nextFileDosPath) {
            this.DosPath = dosPath;
            this.ApplyEnding(file, endingSection, nextFileDosPath);
        }

        public void ApplyDefinition(TapeDumpFile file, in ArcServeFileDefinition definition, long definitionSection, string? lastFileDosPath) {
            if (this.HasDefinition)
                throw new Exception($" - The file declaration was already detected for '{this.DosPath}'.");

            this.Definition = definition;
            this.DefinitionTapeDump = file;
            this.DefinitionSection = definitionSection;
            this.LastFileDosPath = lastFileDosPath;
        }

        public void ApplyEnding(TapeDumpFile file, long endingSection, string? nextFileDosPath) {
            if (this.HasEnding)
                throw new Exception($" - The end of file was already detected for '{this.DosPath}'.");

            this.EndingTapeDump = file;
            this.EndingSection = endingSection;
            this.NextFileDosPath = nextFileDosPath;
        }
    }
}