using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Contains a utility which will merge all of the blocks found inside the parking zone into a single file.
    /// Useful for creating a parking zone file.
    /// </summary>
    public static class ArcServeParkingZoneMerge
    {
        /// <summary>
        /// Merges all of the specified files in the config into a single parking zone file.
        /// </summary>
        /// <param name="tape">The tape configuration to load data for.</param>
        /// <param name="logger">The logger to write data to.</param>
        /// <exception cref="FileNotFoundException">Thrown if any of the specified files do not exist.</exception>
        public static void CreateParkingZoneFile(TapeConfig tape, ILogger logger) {
            List<string> fileList = (from node in tape.Config.Text where !string.IsNullOrWhiteSpace(node?.Value) select node.Value).ToList();
            if (fileList.Count > 0) 
                CreateParkingZoneFile(fileList, tape.FolderPath, logger);
        }

        /// <summary>
        /// Merges all of the specified files in the config into a single parking zone file.
        /// </summary>
        /// <param name="filePaths">Collection of relative file paths to parking dumps.</param>
        /// <param name="folder">The folder file path to run in.</param>
        /// <param name="logger">The logger to write data to.</param>
        /// <exception cref="FileNotFoundException">Thrown if any of the specified files do not exist.</exception>
        public static void CreateParkingZoneFile(IEnumerable<string> filePaths, string folder, ILogger logger) {
            string outputFilePath = Path.Combine(folder, "tape_parking.dump");
            if (File.Exists(outputFilePath)) {
                logger.LogInformation("The parking zone file already exists, so we will not remake it.");
                return;
            }

            logger.LogInformation($"Creating parking zone file '{Path.GetFileName(outputFilePath)}'.");
            using FileStream fileOutStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
            using BufferedStream bufferedOutStream = new BufferedStream(fileOutStream);
            using DataWriter writer = new DataWriter(bufferedOutStream);

            foreach (string relativeFilePath in filePaths) {
                if (string.IsNullOrWhiteSpace(relativeFilePath))
                    continue;
                
                string filePath = Path.Combine(folder, relativeFilePath);
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("Could not find parking zone file.", filePath);
                
                logger.LogInformation($"Reading file '{Path.GetFileName(filePath)}'");

                using FileStream fileInStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using DataReader reader = new DataReader(new BufferedStream(fileInStream));

                string singleFilePath = filePath[..^5] + "-stripped.dump";
                using MemoryStream strippedData = new MemoryStream();
                using DataWriter fileWriter = new DataWriter(strippedData);

                bool anyStripped = false;
                while (reader.HasMore) {
                    reader.JumpTemp(reader.Index + OnStreamDataStream.DataSectionSize + (2 * DataConstants.IntegerSize));
                    uint physicalBlockId = reader.ReadUInt32(ByteEndian.BigEndian);
                    reader.JumpReturn();
                    
                    OnStreamPhysicalPosition.FromPhysicalBlock(physicalBlockId, out OnStreamPhysicalPosition physicalPosition);
                    if (physicalPosition.Location == OnStreamTapeAddressableLocation.ParkingZone) {
                        byte[] blockData = reader.ReadBytes((int)OnStreamDataStream.FullSectionSize);
                        writer.Write(blockData);
                        fileWriter.Write(blockData);
                        logger.LogInformation($" - Copying block '{physicalBlockId:X8}' to parking zone file.");
                    } else {
                        reader.Index += OnStreamDataStream.FullSectionSize;
                        anyStripped = true;
                    }
                }
                
                if (anyStripped)
                    File.WriteAllBytes(singleFilePath, strippedData.ToArray());
            }
            
            logger.LogInformation("Parking zone file created.");
        }
    }
}