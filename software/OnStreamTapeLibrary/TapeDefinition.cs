using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using OnStreamTapeLibrary.Position;
using OnStreamTapeLibrary.Workers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// Represents a tape.
    /// A tape is comprised of any number of tape dump files.
    /// This brings all of the separate tape dumps together, even if multiple files have the same portions of data.
    /// Its purpose is to allow cobbling together various different tape dumps and make it so that they can be treated as if they are one single dump.
    /// As long as this can identify what the block is, it can make a single 
    /// </summary>
    public class TapeDefinition
    {
        public readonly OnStreamCartridgeType Type;
        public readonly string DisplayName;
        public readonly string FolderPath;
        public readonly bool HasOnStreamAuxData;
        public readonly Config Config;
        public readonly List<TapeDumpFile> Entries = new ();
        public readonly HashSet<uint> SkippedPhysicalBlocks = new ();
        public bool ShouldArcServeSkipExtraSectionPerFile { get; init; }

        private TapeDefinition(OnStreamCartridgeType type, string displayName, string folderPath, bool hasOnStreamAuxData, Config config) {
            this.Type = type;
            this.DisplayName = displayName;
            this.Config = config;
            this.FolderPath = folderPath;
            this.HasOnStreamAuxData = hasOnStreamAuxData;
        }
        
        /// <summary>
        /// Find a tape dump by its name or file name.
        /// </summary>
        /// <param name="tapeDumpName">The tape dump to find.</param>
        /// <returns>found tape dump, if any</returns>
        public TapeDumpFile? GetTapeDumpFileByName(string tapeDumpName) {
            if (string.IsNullOrWhiteSpace(tapeDumpName))
                return null;
            
            return this.Entries.Find(file => tapeDumpName.Equals(file.Name, StringComparison.InvariantCulture) 
                || tapeDumpName.Equals(file.FileName, StringComparison.InvariantCulture));
        }
        
        /// <summary>
        /// Creates an ordered list of tape blocks from a mapping of all the present tape blocks.
        /// The order follows logical block order, increasing linearly from zero to the maximum logical block.
        /// </summary>
        /// <param name="blockMapping">The mapping of tape blocks to read from.</param>
        /// <returns>orderedList</returns>
        public List<OnStreamTapeBlock> CreateLogicallyOrderedBlockList(Dictionary<uint, OnStreamTapeBlock> blockMapping) {
            List<OnStreamTapeBlock> blocks = new ();
            OnStreamPhysicalPosition position = this.Type.FromLogicalBlock(0);
            OnStreamPhysicalPosition tempPhysicalPosition = this.Type.CreatePosition();

            OnStreamTapeBlock? lastTapeBlock = null;
            for (int i = 0; i < blockMapping.Count; i++) {
                // Search until the next position with data is found.
                OnStreamTapeBlock? nextTapeBlock;
                while (!blockMapping.TryGetValue(position.ToPhysicalBlock(), out nextTapeBlock)) {
                    if (lastTapeBlock != null) {
                        int oldMissingBlockCount = lastTapeBlock.MissingBlockCount++;
                        if (lastTapeBlock.NextBlockSkipIsSafelySkipped || oldMissingBlockCount == 0) // All missing blocks must be safely skipped.
                            lastTapeBlock.NextBlockSkipIsSafelySkipped = this.SkippedPhysicalBlocks.Contains(position.ToPhysicalBlock()); // Parking zones aren't in the logical block list, so we only test skipped physical blocks.
                    }

                    if (!position.TryIncreaseLogicalBlock())
                        throw new EndOfStreamException($"There are no more logical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
                }

                // Setup next tape block.
                nextTapeBlock.MissingBlockCount = 0;
                if (!this.SkippedPhysicalBlocks.Contains(nextTapeBlock.PhysicalBlock)) {
                    blocks.Add(nextTapeBlock);
                    lastTapeBlock = nextTapeBlock;
                }
                
                // Test if the next block is parking zone.
                tempPhysicalPosition.FromPhysicalBlock(position.ToPhysicalBlock());
                nextTapeBlock.NextBlockSkipIsSafelySkipped = false; // Logical block lists will never have a parking zone as the next block.

                // Increase logical block.
                if (!position.TryIncreaseLogicalBlock())
                    throw new EndOfStreamException($"There are no more logical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
            }

            return blocks;
        }
        
        /// <summary>
        /// Creates an ordered list of tape blocks from a mapping of all the present tape blocks.
        /// The order follows physical block order, going in a straight line until the end/start of the tape is reached.
        /// </summary>
        /// <param name="blockMapping">The mapping of tape blocks to read from.</param>
        /// <returns>orderedList</returns>
        public List<OnStreamTapeBlock> CreatePhysicallyOrderedBlockList(Dictionary<uint, OnStreamTapeBlock> blockMapping) {
            List<OnStreamTapeBlock> blocks = new ();
            OnStreamPhysicalPosition position = this.Type.FromLogicalBlock(0);

            OnStreamTapeBlock? lastTapeBlock = null;
            for (int i = 0; i < blockMapping.Count; i++) {
                // Search until the next position with data is found.
                OnStreamTapeBlock? nextTapeBlock;
                while (!blockMapping.TryGetValue(position.ToPhysicalBlock(), out nextTapeBlock)) {
                    if (lastTapeBlock != null) {
                        int oldMissingBlockCount = lastTapeBlock.MissingBlockCount++;
                        if (lastTapeBlock.NextBlockSkipIsSafelySkipped || oldMissingBlockCount == 0) // All missing blocks must be safely skipped.
                            lastTapeBlock.NextBlockSkipIsSafelySkipped = position.IsParkingZone || this.SkippedPhysicalBlocks.Contains(position.ToPhysicalBlock());
                    }
                    
                    if (!position.TryIncreasePhysicalBlock())
                        throw new EndOfStreamException($"There are no more physical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
                }

                nextTapeBlock.MissingBlockCount = 0;
                nextTapeBlock.NextBlockSkipIsSafelySkipped = false;
                if (!this.SkippedPhysicalBlocks.Contains(nextTapeBlock.PhysicalBlock)) {
                    blocks.Add(nextTapeBlock);
                    lastTapeBlock = nextTapeBlock;
                }

                if (!position.TryIncreasePhysicalBlock())
                    throw new EndOfStreamException($"There are no more physical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
            }

            return blocks;
        }
        
        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() {
            this.Entries.ForEach(entry => entry.Dispose());
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Loads the config from a file path.
        /// </summary>
        /// <param name="configFilePath">The file path to load from.</param>
        /// <param name="logger">The logger to write output to.</param>
        /// <returns>The loaded config, or null if it could not be loaded.</returns>
        public static TapeDefinition? LoadFromConfigFile(string configFilePath, ILogger logger) {
            if (!File.Exists(configFilePath)) {
                logger.LogError("The tape config file '{configFilePath}' does not exist.", configFilePath);
                return null;
            }

            Config tapeCfg = Config.LoadConfigFromFile(configFilePath);
            return LoadFromConfig(tapeCfg, configFilePath, logger);
        }
        
        /// <summary>
        /// Loads the tape config from a <see cref="Config"/>.
        /// </summary>
        /// <param name="config">The configuration data to load from.</param>
        /// <param name="configFilePath">The folder containing the tape dumps.</param>
        /// <param name="logger">The logger to write output to.</param>
        /// <returns>The loaded config, or null if it could not be loaded.</returns>
        public static TapeDefinition? LoadFromConfig(Config config, string configFilePath, ILogger logger) {
            string? tapeFolder = Path.GetDirectoryName(configFilePath);
            if (tapeFolder == null) {
                logger.LogError("Couldn't get directory holding '{configFilePath}'.", configFilePath);
                return null;
            }

            OnStreamCartridgeType type = config.GetValueOrError("type").GetAsEnum<OnStreamCartridgeType>();
            string displayName = config.GetValue("name")?.GetAsString() ?? config.SectionName;
            bool hasAuxData = config.GetValue("hasAuxData")?.GetAsBoolean() ?? (type != OnStreamCartridgeType.Raw);
            TapeDefinition newTapeConfig = new(type, displayName, tapeFolder, hasAuxData, config)
            {
                ShouldArcServeSkipExtraSectionPerFile = config.GetValue("arcServeSkipExtraFileData")?.GetAsBoolean() ?? false
            };

            if (config.HasKey("skip")) {
                string[] skippedStr = config.GetValue("skip").GetAsString().Split(",", StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < skippedStr.Length; i++) {
                    if (UInt32.TryParse(skippedStr[i], out uint parsedBlock)) {
                        newTapeConfig.SkippedPhysicalBlocks.Add(type.ConvertLogicalBlockToPhysicalBlock(parsedBlock));
                        continue;
                    } else if (skippedStr[i].Contains('-', StringComparison.InvariantCultureIgnoreCase)) {
                        string[] split = skippedStr[i].Split('-', 2);
                        if (split.Length == 2 && UInt32.TryParse(split[0], out uint parsedLow) && UInt32.TryParse(split[1], out uint parsedHigh) && parsedHigh >= parsedLow) {
                            for (uint j = parsedLow; j <= parsedHigh; j++)
                                newTapeConfig.SkippedPhysicalBlocks.Add(type.ConvertLogicalBlockToPhysicalBlock(j));
                            continue;
                        }
                    }
                    
                    logger.LogError("Tape was supposed to skip block '{skippedStr}' but it was not a number or a valid range!", skippedStr[i]);
                }
            }
            
            if (config.Text.Count > 0)
                OnStreamParkingZoneMerger.CreateParkingZoneFile(newTapeConfig, logger);

            // Parse the configuration.
            foreach (Config tapeFileEntry in config.ChildConfigs) {
                int? blockIndex = null;
                if (Int32.TryParse(tapeFileEntry.SectionName, out int parsedBlockIndex))
                    blockIndex = parsedBlockIndex;

                TapeDumpFile newFile = new (newTapeConfig, blockIndex);
                if (!newFile.Load(tapeFileEntry, logger)) {
                    logger.LogError("Failed to load tape dump configuration {blockIndex}.", blockIndex);
                    return null;
                }

                newTapeConfig.Entries.Add(newFile);
            }

            return newTapeConfig;
        }
    }
    
    /// <summary>
    /// Represents a tape dump file.
    /// </summary>
    public class TapeDumpFile : IDisposable
    {
        private Stream? _stream;
        private Stream? _rawStream;
        public readonly TapeDefinition Tape;
        public readonly int? BlockIndex;
        public readonly List<uint> Errors = new ();
        public string? Name { get; private set; }
        public string? FileName { get; private set; }
        public string? DumpFilePath { get; private set; }

        public bool HasBlockIndex => (this.BlockIndex != null);

        public Stream Stream => this._stream ?? throw new NullReferenceException("The stream has not been setup.");
        public Stream RawStream => this._rawStream ?? throw new NullReferenceException("The stream has not been setup.");
        
        public TapeDumpFile(TapeDefinition tape, int? position) {
            this.Tape = tape;
            this.BlockIndex = position;
        }

        /// <summary>
        /// Creates a matcher string for testing if a cached block definition matches this block definition.
        /// </summary>
        /// <returns>matcherString</returns>
        public string CreateMatcherString() {
            if (this.DumpFilePath == null)
                throw new DataException("Cannot create matcher string when the file path is null.");
            
            DateTime lastFileWrite = File.GetLastWriteTimeUtc(this.DumpFilePath);
            
            StringBuilder builder = new StringBuilder(this.Name).Append(',')
                .Append(lastFileWrite.ToFileTimeUtc()).Append(',')
                .Append(this._rawStream?.Length ?? -1).Append(',')
                .Append(this.BlockIndex ?? -1);

            // Write errors
            foreach (uint error in this.Errors)
                builder.Append(',').Append(error);
            
            return builder.ToString();
        }

        /// <summary>
        /// Test if a saved string matches the match string for this 
        /// </summary>
        /// <param name="testAgainst">The previously created matcher string to test against.</param>
        /// <returns>Whether the string matches</returns>
        public bool TestMatches(string testAgainst) {
            return string.Equals(this.CreateMatcherString(), testAgainst, StringComparison.InvariantCulture);
        }

        /// <summary>
        /// Loads / initializes the entry.
        /// </summary>
        internal bool Load(Config config, ILogger logger) {
            this.Name = config.SectionName;
            this.DumpFilePath = this.GetFilePath("dump");
            this.FileName = Path.GetFileName(this.DumpFilePath);
            if (!File.Exists(this.DumpFilePath)) {
                logger.LogError("File '{dumpFilePath}' was not found.", this.DumpFilePath);
                return false;
            }

            // Read list of errors.
            foreach (ConfigValueNode value in config.Text) {
                if (string.IsNullOrWhiteSpace(value.Value))
                    continue;

                if (UInt32.TryParse(value.GetAsString(), out uint errorBlock)) {
                    this.Errors.Add(errorBlock);
                } else {
                    throw new DataException($"Cannot interpret '{value.GetAsString()} as a number.");
                }
            }
            this.Errors.Sort();
            
            // Read string.
            FileStream fileStream = new (this.DumpFilePath, FileMode.Open, FileAccess.Read);
            this._rawStream = new BufferedStream(fileStream);
            this._stream = new OnStreamDataStream(this._rawStream);
            return true;
        }


        /// <summary>
        /// Creates a file name for this dump.
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="suffix">The optional suffix to apply to the file name.</param>
        /// <returns>fileName</returns>
        public string GetFileName(string extension, string? suffix = null) {
            return "tape_" + this.Name + (string.IsNullOrWhiteSpace(suffix) ? string.Empty : "_" + suffix) + "." + extension;
        }

        /// <summary>
        /// Creates a file name for this dump, and combines it with the tape config folder path.
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="suffix">The optional suffix to apply to the file name.</param>
        /// <returns>fileName</returns>
        public string GetFilePath(string extension, string? suffix = null) {
            return Path.Combine(this.Tape.FolderPath, this.GetFileName(extension, suffix));
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() {
            this._rawStream?.Dispose();
            this._stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}