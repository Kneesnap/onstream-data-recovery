using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Configuration for tape dumps.
    /// </summary>
    public class TapeConfig : IDisposable
    {
        public readonly string DisplayName;
        public readonly string FolderPath;
        public readonly Config Config;
        public readonly List<TapeDumpFile> Entries = new List<TapeDumpFile>();
        public readonly HashSet<uint> SkippedPhysicalBlocks = new HashSet<uint>();

        private TapeConfig(string displayName, string folderPath, Config config) {
            this.DisplayName = displayName;
            this.Config = config;
            this.FolderPath = folderPath;
        }

        /// <summary>
        /// Loads the config from a file path.
        /// </summary>
        /// <param name="configFilePath">The file path to load from.</param>
        /// <param name="logger">The logger to write output to.</param>
        /// <returns>The loaded config, or null if it could not be loaded.</returns>
        public static TapeConfig? LoadFromConfigFile(string configFilePath, ILogger logger) {
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
        public static TapeConfig? LoadFromConfig(Config config, string configFilePath, ILogger logger) {
            string? tapeFolder = Path.GetDirectoryName(configFilePath);
            if (tapeFolder == null) {
                logger.LogError($"Couldn't get directory holding '{configFilePath}'.");
                return null;
            }

            string displayName = config.GetValue("name")?.GetAsString() ?? config.SectionName;
            TapeConfig newTapeConfig = new TapeConfig(displayName, tapeFolder, config);

            if (config.HasKey("skip")) {
                string[] skippedStr = config.GetValue("skip").GetAsString().Split(",", StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < skippedStr.Length; i++) {
                    if (UInt32.TryParse(skippedStr[i], out uint parsedBlock)) {
                        newTapeConfig.SkippedPhysicalBlocks.Add(OnStreamPhysicalPosition.ConvertLogicalBlockToPhysical(parsedBlock));
                    } else {
                        logger.LogError($"Tape was supposed to skip block '{skippedStr[i]}' but it was not a number!");
                    }
                }
            }
            
            if (config.Text.Count > 0)
                ArcServeParkingZoneMerge.CreateParkingZoneFile(newTapeConfig, logger);

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

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() {
            this.Entries.ForEach(entry => entry.Dispose());
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents a tape dump file.
    /// </summary>
    public class TapeDumpFile : IDisposable
    {
        private Stream? _stream;
        private Stream? _rawStream;
        public readonly TapeConfig TapeConfig;
        public readonly int? BlockIndex;
        public readonly List<uint> Errors = new List<uint>();
        public string? Name { get; private set; }
        public string? FileName { get; private set; }

        public bool HasBlockIndex => (this.BlockIndex != null);

        public Stream Stream => this._stream ?? throw new NullReferenceException("The stream has not been setup.");
        public Stream RawStream => this._rawStream ?? throw new NullReferenceException("The stream has not been setup.");
        
        public TapeDumpFile(TapeConfig parentCfg, int? position) {
            this.TapeConfig = parentCfg;
            this.BlockIndex = position;
        }

        /// <summary>
        /// Loads / initializes the entry.
        /// </summary>
        internal bool Load(Config config, ILogger logger) {
            this.Name = config.SectionName;
            string dumpFilePath = this.GetFilePath("dump");
            this.FileName = Path.GetFileName(dumpFilePath);
            if (!File.Exists(dumpFilePath)) {
                logger.LogError($"File '{dumpFilePath}' was not found.");
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
            FileStream fileStream = new FileStream(dumpFilePath, FileMode.Open, FileAccess.Read);
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
            return Path.Combine(this.TapeConfig.FolderPath, this.GetFileName(extension, suffix));
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose() {
            this._rawStream?.Dispose();
            this._stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}