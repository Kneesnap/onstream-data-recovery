namespace OnStreamTapeLibrary
{
    /// <summary>
    /// Uniquely identifies a 32Kb block of data from a tape dump.
    /// </summary>
    public class OnStreamTapeBlock
    {
        public readonly TapeDumpFile File;
        public readonly uint Signature;
        public readonly long Index; // File position of the block data within the file it was found in.
        public readonly uint PhysicalBlock; // The physical position on the tape which the block corresponds to.
        public int MissingBlockCount; // The number of blocks which are missing between this tape block and the next seen tape block.
        public uint LogicalBlock => this.File.Tape.Type.ConvertPhysicalBlockToLogicalBlock(this.PhysicalBlock);
        public string LogicalBlockString => this.File.Tape.Type.ConvertPhysicalBlockToLogicalBlockString(this.PhysicalBlock);
        
        public OnStreamTapeBlock(TapeDumpFile file, long index, uint signature, uint physicalBlock) {
            this.File = file;
            this.Index = index;
            this.PhysicalBlock = physicalBlock;
            this.Signature = signature;
        }

        /// <summary>
        /// Creates a string representation of this block.
        /// </summary>
        /// <returns>blockAsString</returns>
        public string Serialize() {
            return $"{this.File.Name},{this.Index},{this.PhysicalBlock},{this.Signature}";
        }

        public static bool TryParse(TapeDefinition tape, string input, out OnStreamTapeBlock? result) {
            result = null;
            string[] split = input.Split(',');
            
            // Find tape file.
            string fileName = split[0];
            TapeDumpFile? foundFile = tape.GetTapeDumpFileByName(fileName);
            if (foundFile == null)
                return false;

            // Parse numbers.
            if (!long.TryParse(split[1], out long fileIndex))
                return false;
            if (!uint.TryParse(split[2], out uint physicalBlock))
                return false;
            if (!uint.TryParse(split[3], out uint signature))
                return false;
            
            // Done.
            result = new OnStreamTapeBlock(foundFile, fileIndex, signature, physicalBlock);
            return true;
        }
    }
}