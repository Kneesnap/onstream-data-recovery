using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents the end of a tape session.
    /// This is something of a weird chunk of data. Not sure why it's organized seemingly randomly.
    /// </summary>
    public class ArcServeSessionEndPacket : ArcServeFilePacket
    {
        public uint Unknown0 { get; private set; } // I've only observed this to be zero, but everything else is 7E, so that means this is probably a real data field.
        public uint CatalogFilePageIndex { get; private set; }
        public uint CatalogFilePageOffset { get; private set; }
        public uint Unknown1 { get; private set; } // This has some low number. I've seen 3, 5, etc. Haven't yet figured out what it's for, but I assume it's information about the session, previous session, or next session. Shrug.

        /// <summary>
        /// Calculates the full raw file index (from the tape origin) of the start of the catalog file for this session.
        /// </summary>
        public long CatalogFileRawIndex => ((long) this.CatalogFilePageIndex * ArcServeCatalogueFileEntry.PageSizeInBytes) + this.CatalogFilePageOffset;
        
        public const uint PacketSignature = 0x7E7E7E7E;
        public const byte PaddingByte = 0x7E;
        
        public ArcServeSessionEndPacket(ArcServeTapeArchive tapeArchive) : base(tapeArchive, PacketSignature)
        {
        }

        public override bool AppearsValid => !this.EncounteredErrorWhileLoading;
        
        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader)
        {
            reader.SkipBytesRequire(PaddingByte, 288);
            this.Unknown0 = reader.ReadUInt32();
            this.CatalogFilePageIndex = reader.ReadUInt32(ByteEndian.LittleEndian);
            this.CatalogFilePageOffset = reader.ReadUInt32(ByteEndian.LittleEndian);
            this.Unknown1 = reader.ReadUInt32();
            reader.SkipBytesRequire(PaddingByte, 39);
        }

        /// <inheritdoc cref="ArcServeFilePacket.WriteInformation"/>
        public override void WriteInformation(DataReader? reader)
        {
            this.Logger.LogInformation("==================================================");
            this.Logger.LogInformation("                 TAPE SESSION END                 ");
            this.Logger.LogInformation(string.Empty);
            this.Logger.LogInformation("Catalog Raw File Index: {rawFileIndex}", reader.GetFileIndexDisplay(this.CatalogFileRawIndex));
            this.Logger.LogInformation("Unknown 0: {unknown0}, Unknown 1: {unknown1}", this.Unknown0, this.Unknown1);
            this.Logger.LogInformation("==================================================");
        }

        /// <inheritdoc cref="ArcServeFilePacket.Process"/>
        public override bool Process(DataReader reader) => true; // Do nothing.
    }
}