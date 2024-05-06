using System.Text;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// A data packet representing the end of a file.
    /// </summary>
    public class ArcServeFileTrailer : ArcServeFilePacket
    {
        public const uint FileTrailerSignature = 0xCCCCCCCCU;
        public string RelativeFilePath { get; private set; } = string.Empty; // Relative to the root directory, 
        public uint CrcChecksum { get; private set; }
        public byte Unknown { get; private set; }

        /// <inheritdoc cref="ArcServeFilePacket.AppearsValid"/>
        public override bool AppearsValid => ArcServe.IsValidLookingString(this.RelativeFilePath) || (string.IsNullOrEmpty(this.RelativeFilePath) && this.CrcChecksum == 0 && this.Unknown == 0);

        public ArcServeFileTrailer(ArcServeTapeArchive tapeArchive) : base(tapeArchive, FileTrailerSignature)
        {
        }
        
        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader) {
            this.RelativeFilePath = reader.ReadFixedSizeString(246);
            this.CrcChecksum = reader.ReadUInt32();
            this.Unknown = reader.ReadByte();
            reader.SkipBytesRequireEmpty(257);
            // TODO: If this is a file (not a directory), check CRC hash matches. (Unless CRC hash is zero..?)
        }

        /// <inheritdoc cref="ArcServeFilePacket.WriteInformation"/>
        public override void WriteInformation(DataReader reader) {
            StringBuilder builder = new (" - Reached End of File");
            if (!string.IsNullOrEmpty(this.RelativeFilePath))
                builder.AppendFormat(": {0}", this.RelativeFilePath);
            if (this.CrcChecksum != 0)
                builder.AppendFormat(", Hash: {0:X8}", this.CrcChecksum);
            if (this.Unknown != 0)
                builder.AppendFormat(", Unknown: {0:X}", this.Unknown);
            
            this.Logger.LogInformation("{information}", builder);
        }
        
        /// <inheritdoc cref="ArcServeFilePacket.Process"/>
        public override bool Process(DataReader reader)
        {
            return true; // Processing does nothing and thus always succeeds.
        }
    }
}