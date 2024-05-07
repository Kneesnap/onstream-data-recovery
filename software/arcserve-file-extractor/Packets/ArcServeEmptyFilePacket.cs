using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents a packet known to be empty.
    /// We keep this disabled for logging purposes.
    /// </summary>
    public class ArcServeEmptyFilePacket : ArcServeFilePacket
    {
        private int _nonZeroBytesFound;
        
        public ArcServeEmptyFilePacket(ArcServeTapeArchive tapeArchive) : base(tapeArchive, 0)
        {
        }

        /// <inheritdoc cref="ArcServeFilePacket.AppearsValid"/>
        public override bool AppearsValid => true;
        
        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader)
        {
            long currentIndex = reader.Index;
            reader.AlignRequireEmpty(sizeof(uint));

            // Find bytes required to align to the next sector.
            long difference = (currentIndex % ArcServe.RootSectorSize);
            if (difference == 0)
                return; // No need to do anything?

            long byteIncrease = (ArcServe.RootSectorSize - difference);
            for (int i = 0; i < byteIncrease / sizeof(uint); i++)
            {
                uint nextChunk = reader.ReadUInt32();
                if (nextChunk == 0)
                    continue;

                if ((nextChunk & 0xFF) != 0)
                    this._nonZeroBytesFound++;
                if ((nextChunk & 0xFF00) != 0)
                    this._nonZeroBytesFound++;
                if ((nextChunk & 0xFF0000) != 0)
                    this._nonZeroBytesFound++;
                if ((nextChunk & 0xFF000000) != 0)
                    this._nonZeroBytesFound++;
            }
        }

        /// <inheritdoc cref="ArcServeFilePacket.WriteInformation"/>
        public override void WriteInformation(DataReader? reader)
        {
            if (this._nonZeroBytesFound > 0)
                this.Logger.LogError("File Packet starting at {filePacketStartIndex} was expected to be completely empty, but had {byteCount} non-zero bytes!", reader.GetFileIndexDisplay(this.ReaderStartIndex), this._nonZeroBytesFound);
        }

        /// <inheritdoc cref="ArcServeFilePacket.Process"/>
        public override bool Process(DataReader reader)
        {
            return this._nonZeroBytesFound == 0; // Only reset the skipped section count if this actually appears to be empty.
        }
    }
}