using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.DataStructures;
using OnStreamTapeLibrary.Position;
using System.Collections.Generic;

namespace OnStreamTapeLibrary.Workers
{
    /// <summary>
    /// Finds gaps in data dumps.
    /// </summary>
    public static class OnStreamGapFinder
    {
        /// <summary>
        /// Find and log gaps in the tape data layout.
        /// </summary>]
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="blockMapping">The mapping of blocks.</param>
        /// <param name="logger">The logger to log with.</param>
        public static void FindAndLogGaps(OnStreamCartridgeType type, Dictionary<uint, OnStreamTapeBlock> blockMapping, ILogger logger) {
            BitArray array = new BitArray((int)(type.GetTrackFrameCount() * type.GetLogicalTrackCount()));
            OnStreamPhysicalPosition tempPos = type.CreatePosition();
            foreach (uint physicalPosition in blockMapping.Keys) {
                tempPos.FromPhysicalBlock(physicalPosition);
                int tempValue = (int)(tempPos.Y * type.GetTrackFrameCount()) + tempPos.X;
                array.Set(tempValue, true);
            }

            OnStreamPhysicalPosition currentPos = type.FromLogicalBlock(0); // Tape origin. Includes the parking zone.
            List<TapeEmptyGap> gaps = new List<TapeEmptyGap>();
            
            int gapSize = 0;
            OnStreamPhysicalPosition firstOpenPhysPos = currentPos.Clone();
            do {
                int arrayIndex = (int)(currentPos.Y * type.GetTrackFrameCount()) + currentPos.X;
                if (!array.Get(arrayIndex)) {
                    if (gapSize++ == 0)
                        firstOpenPhysPos.CopyFrom(currentPos);
                    continue;
                }

                if (gapSize > 0) {
                    gaps.Add(new TapeEmptyGap(firstOpenPhysPos, currentPos, gapSize));
                    gapSize = 0;
                }
            } while (currentPos.TryIncreasePhysicalBlock());
            
            // Sort first by X position, then by track.
            // The purpose of this is to organize data located physically together, so when using this as a list of data to dump,
            //  it will organize the data together to reduce the amount of seeking required
            // And also when I dump data based on this list it's nice to have this data together because it
            //  allows me to avoid seeking constantly by organizing all the data in a certain area
            gaps.Sort(Comparer<TapeEmptyGap>.Create((a, b) =>
            {
                int aPosX = (a.StartPos.X + a.EndPos.X); // Average it out so that things on different tracks appear together.
                int bPosX = (b.StartPos.X + b.EndPos.X);
                var res= aPosX.CompareTo(bPosX);
                return res != 0 ? res : a.StartPos.Y.CompareTo(b.StartPos.Y);
            }));
            
            if (gapSize > 0) // Add this after the sorting, so this shows up last.
                gaps.Add(new TapeEmptyGap(firstOpenPhysPos, currentPos, gapSize));

            if (gaps.Count > 0) {
                logger.LogInformation("Missing Data Ranges (No data is here, it could be damaged, or maybe there's just no data written there):");

                // Display the gaps.
                foreach (TapeEmptyGap gap in gaps) 
                    logger.LogInformation($" - Gap Position: {gap.StartPos.ToPhysicalBlockString()} -> {gap.EndPos.ToPhysicalBlockString()}, Logical Blocks: {gap.StartPos.ToLogicalBlockString()} -> {gap.EndPos.ToLogicalBlockString()}, Gap Size (Blocks): {gap.BlockCount}");

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
            
            public TapeEmptyGap(OnStreamPhysicalPosition startPos, OnStreamPhysicalPosition endPos, int blockCount) {
                this.StartPos = startPos.Clone();
                this.EndPos = endPos.Clone();
                this.BlockCount = blockCount;
            }
        }
    }
}