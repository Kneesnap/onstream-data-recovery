using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamSCArcServeExtractor.Packets;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// This systems allows reading ArcServe file packets.
    /// </summary>
    public class ArcServeFilePacketReader
    {
        public readonly ArcServeTapeArchive TapeArchive;
        public readonly DataReader Reader;
        public ArcServeSessionHeader? LastSessionHeader;
        public long SkippedSectorCount;
        public long FirstSkippedSectorIndex = -1;
        public uint FirstSkippedSectorSignature = 0xBADDEAD;

        public ArcServeFilePacketReader(ArcServeTapeArchive tapeArchive, DataReader reader)
        {
            this.TapeArchive = tapeArchive;
            this.Reader = reader;
        }

        /// <summary>
        /// Shows the skipped sector information, resetting it.
        /// </summary>
        public void ShowSkippedSectorInfo()
        {
            if (this.SkippedSectorCount <= 0)
                return;

            this.TapeArchive.Logger.LogInformation("Skipped {skipCount} packet section(s) starting at {readerIndex}/{signature:X8} did not appear to be valid. (End Index: {endReaderIndex})", this.SkippedSectorCount, this.FirstSkippedSectorSignature, this.Reader.GetFileIndexDisplay(this.FirstSkippedSectorIndex), this.Reader.GetFileIndexDisplay());
            this.SkippedSectorCount = 0;
            this.FirstSkippedSectorIndex = -1;
            this.FirstSkippedSectorSignature = 0xBADDEAD;
        }

        /// <summary>
        /// Indicate that a sector has been skipped.
        /// </summary>
        public void MarkSkippedSector(uint signature)
        {
            if (this.SkippedSectorCount++ > 0)
                return;

            this.FirstSkippedSectorIndex = this.Reader.Index;
            this.FirstSkippedSectorSignature = signature;
        }

        /// <summary>
        /// Attempts to read a file packet from the reader.
        /// </summary>
        /// <param name="packetSignature"></param>
        /// <returns></returns>
        public bool TryReadPacket(uint packetSignature)
        {
            if (packetSignature == 0)
                return true; // We don't load a packet, but we also don't consider this a failure. We're just going to let it skip ahead.

            ArcServeFilePacket? newPacket = ArcServeFilePacket.CreateFilePacketFromSignature(this.TapeArchive, this.LastSessionHeader, packetSignature);
            if (newPacket == null)
                return false; // Signature wasn't a recognized packet.

            this.ShowSkippedSectorInfo();
            this.TapeArchive.OrderedPackets.Add(newPacket);
            long packetReadStartIndex = this.Reader.Index;
            try {
                newPacket.LoadFromReader(this.Reader); // Load from the reader.
            } catch {
                newPacket.EncounteredErrorWhileLoading = true;

                // Ensure we can see what actually caused the error.
                if (newPacket.AppearsValid) {
                    newPacket.WriteInformation();
                } else {
                    // Avoid printing garbage text characters if we can avoid it. It's not a huge deal but it can be annoying.
                    this.TapeArchive.Logger.LogError("Failed to read packet of type {packetType} from {startIndex}. (The data was too broken to display)", newPacket.GetTypeDisplayName(), this.Reader.GetFileIndexDisplay(packetReadStartIndex));
                }

                throw;
            }

            // If the packet looks like a valid packet, handle it.
            bool loadSuccess = false;
            if (newPacket.AppearsValid) {
                newPacket.WriteInformation();
                loadSuccess = newPacket.Process(this.Reader);
                if (loadSuccess && newPacket is ArcServeSessionHeader sessionHeaderPacket)
                    this.LastSessionHeader = sessionHeaderPacket;
            }

            return loadSuccess;
        }
    }
}