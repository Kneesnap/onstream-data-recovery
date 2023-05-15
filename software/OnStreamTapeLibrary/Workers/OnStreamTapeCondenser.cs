using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using System.Collections.Generic;
using System.IO;

namespace OnStreamTapeLibrary.Workers
{
    /// <summary>
    /// Condenses all of the tape dump files together into a single tape dump file, with the expected order.
    /// </summary>
    public static class OnStreamTapeCondenser
    {
        private const string CondensedTapeFileName = "tape_full.dump";
        
        /// <summary>
        /// Condense all of the tape dumps into a single file.
        /// </summary>
        /// <param name="tape">The tape to condense.</param>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="orderedBlocks">A list of the blocks to write, in the order to write them.</param>
        public static void Condense(TapeDefinition tape, ILogger logger, List<OnStreamTapeBlock> orderedBlocks) {
            if (tape.Entries.Count <= 1 || tape.GetTapeDumpFileByName(CondensedTapeFileName) != null)
                return; // Don't condense, there's only one file, or the file is used.
            
            string condensedFilePath = Path.Combine(tape.FolderPath, CondensedTapeFileName);
            if (File.Exists(condensedFilePath)) {
                logger.LogInformation("The tape dumps have already been condensed.");
                return;
            }

            logger.LogInformation("Condensing all of the tape dump files into a single file.");
            using FileStream fileStream = new FileStream(condensedFilePath, FileMode.Create, FileAccess.Write);
            using BufferedStream bufferedStream = new BufferedStream(fileStream);
            using DataWriter writer = new DataWriter(bufferedStream);

            foreach (OnStreamTapeBlock block in orderedBlocks) {
                using DataReader reader = new DataReader(block.File.RawStream, true);
                reader.Index = OnStreamDataStream.AddAuxSectionsToIndex(block.Index);
                writer.Write(reader.ReadBytes((int)OnStreamDataStream.FullSectionSize));
            }
            
            logger.LogInformation($"Finished condensing {orderedBlocks.Count} tape blocks into '{CondensedTapeFileName}'.");
        }
    }
}