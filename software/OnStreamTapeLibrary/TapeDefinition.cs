﻿using Microsoft.Extensions.Logging;
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
        public readonly Config Config;
        public readonly List<TapeDumpFile> Entries = new List<TapeDumpFile>();
        public readonly HashSet<uint> SkippedPhysicalBlocks = new HashSet<uint>();

        private TapeDefinition(OnStreamCartridgeType type, string displayName, string folderPath, Config config) {
            this.Type = type;
            this.DisplayName = displayName;
            this.Config = config;
            this.FolderPath = folderPath;
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
            List<OnStreamTapeBlock> blocks = new List<OnStreamTapeBlock>();
            OnStreamPhysicalPosition position = this.Type.FromLogicalBlock(0);

            OnStreamTapeBlock? lastTapeBlock = null;
            for (int i = 0; i < blockMapping.Count; i++) {
                // Search until the next position with data is found.
                OnStreamTapeBlock? nextTapeBlock;
                while (!blockMapping.TryGetValue(position.ToPhysicalBlock(), out nextTapeBlock)) {
                    if (lastTapeBlock != null)
                        lastTapeBlock.MissingBlockCount++;
                    if (!position.TryIncreaseLogicalBlock())
                        throw new EndOfStreamException($"There are no more logical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
                }

                nextTapeBlock.MissingBlockCount = 0;
                if (!this.SkippedPhysicalBlocks.Contains(nextTapeBlock.PhysicalBlock)) {
                    blocks.Add(nextTapeBlock);
                    lastTapeBlock = nextTapeBlock;
                }

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
            List<OnStreamTapeBlock> blocks = new List<OnStreamTapeBlock>();
            OnStreamPhysicalPosition position = this.Type.FromLogicalBlock(0);

            OnStreamTapeBlock? lastTapeBlock = null;
            for (int i = 0; i < blockMapping.Count; i++) {
                // Search until the next position with data is found.
                OnStreamTapeBlock? nextTapeBlock;
                while (!blockMapping.TryGetValue(position.ToPhysicalBlock(), out nextTapeBlock)) {
                    if (lastTapeBlock != null)
                        lastTapeBlock.MissingBlockCount++;
                    if (!position.TryIncreasePhysicalBlock())
                        throw new EndOfStreamException($"There are no more physical blocks, and you've found {blocks.Count} blocks when there are actually {blockMapping.Count}.");
                }

                nextTapeBlock.MissingBlockCount = 0;
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
                logger.LogError($"The tape config file '{configFilePath}' does not exist.");
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
                logger.LogError($"Couldn't get directory holding '{configFilePath}'.");
                return null;
            }

            OnStreamCartridgeType type = config.GetValueOrError("type").GetAsEnum<OnStreamCartridgeType>();
            string displayName = config.GetValue("name")?.GetAsString() ?? config.SectionName;
            TapeDefinition newTapeConfig = new TapeDefinition(type, displayName, tapeFolder, config);

            if (config.HasKey("skip")) {
                string[] skippedStr = config.GetValue("skip").GetAsString().Split(",", StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < skippedStr.Length; i++) {
                    if (UInt32.TryParse(skippedStr[i], out uint parsedBlock)) {
                        newTapeConfig.SkippedPhysicalBlocks.Add(type.ConvertLogicalBlockToPhysicalBlock(parsedBlock));
                    } else {
                        logger.LogError($"Tape was supposed to skip block '{skippedStr[i]}' but it was not a number!");
                    }
                }
            }
            
            if (config.Text.Count > 0)
                OnStreamParkingZoneMerger.CreateParkingZoneFile(newTapeConfig, logger);

            // Parse the configuration.
            foreach (Config tapeFileEntry in config.ChildConfigs) {
                int? blockIndex = null;
                if (Int32.TryParse(tapeFileEntry.SectionName, out int parsedBlockIndex))
                    blockIndex = parsedBlockIndex;

                TapeDumpFile newFile = new TapeDumpFile(newTapeConfig, blockIndex);
                if (!newFile.Load(tapeFileEntry, logger)) {
                    logger.LogError($"Failed to load tape dump configuration {blockIndex}.");
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
        public readonly List<uint> Errors = new List<uint>();
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
        public String CreateMatcherString() {
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
                logger.LogError($"File '{this.DumpFilePath}' was not found.");
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
            FileStream fileStream = new FileStream(this.DumpFilePath, FileMode.Open, FileAccess.Read);
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