using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OnStreamTapeLibrary.Workers
{
    /// <summary>
    /// Generates block mappings of physical blocks to tape block object representations.
    /// </summary>
    public static class OnStreamBlockMapping
    {
        /// <summary>
        /// Load or generate a block mapping.
        /// </summary>
        /// <param name="tape">The tape to get the block mapping for.</param>
        /// <param name="logger">The logger to write data to.</param>
        /// <returns>blockMapping</returns>
        public static Dictionary<uint, OnStreamTapeBlock> GetBlockMapping(TapeDefinition tape, ILogger logger) {
            string cacheFilePath = Path.Combine(tape.FolderPath, "blocks.cache");

            if (TryLoadBlockMappingFromCache(tape, logger, cacheFilePath, out var blockMapping) && blockMapping != null)
                return blockMapping;
            
            blockMapping = GenerateBlockMapping(tape, logger);
            SaveBlockMappingCache(tape, logger, blockMapping, cacheFilePath);
            return blockMapping;
        }
        
        /// <summary>
        /// Generate block mapping of physical block to the tape block object.
        /// </summary>
        /// <param name="tape">The tape definition to generate a block mapping for.</param>
        /// <param name="logger">The logger to log information to.</param>
        /// <returns>blockMapping</returns>
        private static Dictionary<uint, OnStreamTapeBlock> GenerateBlockMapping(TapeDefinition tape, ILogger logger) {
            Dictionary<uint, OnStreamTapeBlock> blockMap = new Dictionary<uint, OnStreamTapeBlock>();

            logger.LogInformation("Scanning tape chunks to map out their contents, this may take a while...");
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

                    uint marker = reader.ReadUInt32(ByteEndian.BigEndian); // NOTE! See below.
                    uint physicalPosition = reader.ReadUInt32(ByteEndian.BigEndian); // NOTE! See below.
                    reader.Index += OnStreamDataStream.AuxSectionSize - (3 * DataConstants.IntegerSize);
                    
                    // Note:
                    // The documentation lists this field as the "Hardware Field" and explicitly says that this field has undefined behavior on reads.
                    // However, in ALL tapes I have dumped I have dumped (different software, different size, even one written with an ADR50), this field seems to return the physical block position of the block which was read.
                    // This is incredibly useful for automatically stitching together different tape dumps so the data is automatically read in the proper order without the person dumping the data needing to do anything fancy.
                    // It should be kept in mind that this is undefined behavior, and that if it is observed that if this field's behavior differs, this section may need to be modified.
                    
                    // Another Note:
                    // The application signature is an identifier of the software used to write the data to the tape.
                    // However, there is some strange behavior observed with the signature 0x57545354, or what I am calling the "Write Stop" identifier.
                    // This identifier is seem across multiple pieces of software, and does not appear to be actually chosen by the software.
                    // On the retrospect tapes, "Write Stop" seemed to indicate that a portion of tape would be skipped.
                    // With ArcServe, a much higher amount of write stops signature are seen after the end of the tape data.
                    // The current hypothesis is that write stops might be the default data on the tape from the factory, and usually the drive will not return such data, but in some rare (currently not understood) situations it will.
                    // There have been no occurrences seen where there is valid data in a write stop block.
                    // This suggests Write Stop is something managed by the tape drive itself, but as its behavior is not fully understood, we leave it alone so something else can handle it better.

                    // Determine position:
                    if ((physicalPosition != 0 && physicalPosition != 0xFFFFFFFFU) || (marker != 0 && marker != 0xFFFFFFFFU)) { // This happened with one of the frogger 2 dumps.
                        // Do nothing, we're good.
                    } else {
                        reader.JumpTemp(fileIndex);
                        byte[] userData = reader.ReadBytes((int)OnStreamDataStream.DataSectionSize);
                        reader.JumpReturn();

                        bool foundData = false;
                        for (int i = 0; i < userData.Length && !foundData; i++)
                            if (userData[i] != 0)
                                foundData = true;

                        if (!foundData) {
                            logger.LogWarning($" - {reader.GetFileIndexDisplay(fileIndex)} in {entry.FileName} appears to be missing, likely dumped with old dumping program. (Probably block {logicalPosition})");
                            logicalPosition++;
                            continue;
                        } else if (entry.HasBlockIndex) { 
                            logger.LogWarning($" - {reader.GetFileIndexDisplay(fileIndex)} in {entry.FileName} reported an invalid physical position. We've calculated it to be {logicalPosition} instead."); 
                            physicalPosition = tape.Type.ConvertLogicalBlockToPhysicalBlock(logicalPosition); 
                        } else { 
                            logger.LogWarning($" - {reader.GetFileIndexDisplay(fileIndex)} in {entry.FileName} reported an invalid physical position, and was skipped because there was no base block index.");
                            logicalPosition++; 
                            continue; 
                        } 
                    }
                    
                    // Track the block.
                    blockMap[physicalPosition] = new OnStreamTapeBlock(entry, fileIndexWithoutAux, marker, physicalPosition);
                    logicalPosition++;
                }

                reader.JumpReturn();
            }

            logger.LogInformation($"Scan complete, mapped {blockMap.Count} blocks.");
            return blockMap;
        }

        /// <summary>
        /// Attempts to load the block mapping from the file cache.
        /// </summary>
        /// <param name="tape">The tape to load the mapping for.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="cacheFilePath">The file path of the cache.</param>
        /// <param name="results">The storage to save</param>
        /// <returns>Whether the cache was loaded.</returns>
        private static bool TryLoadBlockMappingFromCache(TapeDefinition tape, ILogger logger, string cacheFilePath, out Dictionary<uint, OnStreamTapeBlock>? results) {
            if (!File.Exists(cacheFilePath)) {
                logger.LogInformation("The block mapping was not found, so it will be generated.");
                results = null;
                return false;
            }
            
            logger.LogInformation("Attempting to load previously saved tape block mapping...");
            Config cacheCfg = Config.LoadConfigFromFile(cacheFilePath);

            // Find files 
            int filesFound = 0;
            Config fileListCfg = cacheCfg.GetChildConfigByName("Files");
            foreach (ConfigValueNode node in fileListCfg.Text) {
                string value = node.Value;
                if (string.IsNullOrWhiteSpace(value))
                    continue;
                
                if (tape.Entries.Any(entry => entry.TestMatches(value))) {
                    filesFound++;
                } else {
                    logger.LogInformation($"The block mapping for '{value}' is no longer valid, so the mapping will be remade.");
                    results = null;
                    return false;
                }
            }

            if (filesFound != tape.Entries.Count) {
                logger.LogInformation("The block mapping is no longer valid, it will be remade.");
                results = null;
                return false;
            }
            
            // Load all of the blocks
            Dictionary<uint, OnStreamTapeBlock> blockMapping = new Dictionary<uint, OnStreamTapeBlock>();
            foreach (ConfigValueNode entry in cacheCfg.Text) {
                if (string.IsNullOrWhiteSpace(entry.Value))
                    continue;

                if (OnStreamTapeBlock.TryParse(tape, entry.Value, out OnStreamTapeBlock? block) && block != null) {
                    blockMapping[block.PhysicalBlock] = block;
                } else {
                    logger.LogError($"Failed to load cached block '{entry.Value}'.");
                    results = null;
                    return false;
                }
            }
            
            // Done
            logger.LogInformation("Successfully loaded the previous tape block mapping.");
            results = blockMapping;
            return true;
        }

        /// <summary>
        /// Saves the block mapping to a file, so it can be read later.
        /// </summary>
        /// <param name="tape">The tape to save the cache for.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="blockMapping">The block mapping to save.</param>
        /// <param name="cacheFilePath">The path to the cache file.</param>
        private static void SaveBlockMappingCache(TapeDefinition tape, ILogger logger, Dictionary<uint, OnStreamTapeBlock> blockMapping, string cacheFilePath) {
            logger.LogInformation("Saving tape block mapping to cache file...");
            
            // Save blocks.
            Config cacheCfg = new Config();
            foreach (OnStreamTapeBlock block in blockMapping.Values)
                cacheCfg.InternalText.Add(new ConfigValueNode(block.Serialize(), null));

            // Save files.
            Config fileListCfg = new Config(cacheCfg);
            fileListCfg.SectionName = "Files";
            foreach (TapeDumpFile tapeFile in tape.Entries)
                fileListCfg.InternalText.Add(new ConfigValueNode(tapeFile.CreateMatcherString(), null));

            cacheCfg.InternalChildConfigs.Add(fileListCfg);

            // Save file.
            File.WriteAllText(cacheFilePath, cacheCfg.ToString());
        }
    }
}