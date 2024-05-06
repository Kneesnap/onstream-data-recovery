using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Extensions;
using OnStreamTapeLibrary;
using TestBed;

namespace OnStreamSCArcServeExtractor
{
    public static class ArcServeStreamDataTypes
    {
        private static readonly Dictionary<uint, Type> PacketTypes = new Dictionary<uint, Type>();
        
        static ArcServeStreamDataTypes() {
            PacketTypes[0x00000000U] = typeof(ArcServeStreamEndData);
            PacketTypes[0x0100DAADU] = typeof(ArcServeStreamRawData);
            PacketTypes[0x3010DAADU] = typeof(ArcServeStreamCatalogueData); // Seems to be used with catalogue files.
            PacketTypes[0x2110DAADU] = typeof(ArcServeStreamWindowsFileName);
        }

        /// <summary>
        /// Creates a new streaming data packet from the provided definition.
        /// </summary>
        /// <param name="block">The block to load from.</param>
        /// <returns>newPacket</returns>
        /// <exception cref="InvalidCastException">Thrown if the registered data type is invalid.</exception>
        public static ArcServeStreamData CreatePacket(in ArcServeStreamHeader block) {
            switch (block.Type) {
                case (uint)ArcServeStreamType.DosPath:
                    return new ArcServeStreamWindowsFileName();
                case (uint)ArcServeStreamType.FullPath:
                    return new ArcServeStreamFullPathData();
            }
            
            if (!PacketTypes.TryGetValue(block.Id, out Type? packetType))
                return new ArcServeUnsupportedStreamData();

            object? newObject = Activator.CreateInstance(packetType);
            if (newObject is ArcServeStreamData newPacket)
                return newPacket;

            throw new InvalidCastException($"The type {packetType} is not an ArcServe packet type!");
        }
    }

    public abstract class ArcServeStreamData
    {
        public ArcServeStreamHeader Block;
        public long BodyStartIndex;
        public long BodyEndIndex;
        
        /// <summary>
        /// Loads the stream data from the reader.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="block">The block to read.</param>
        public virtual void LoadFromReader(DataReader reader, in ArcServeStreamHeader block) {
            this.Block = block;
        }

        /// <summary>
        /// Gets extra information to log to the logger.
        /// </summary>
        /// <returns>the extra information to log.</returns>
        public virtual string GetExtraLoggerInfo() => string.Empty;

        /// <summary>
        /// Write packet read info to the logger.
        /// </summary>
        /// <param name="logger">The logger to write the data to.</param>
        /// <param name="reader">The reader to calculate read positions with.</param>
        public void WritePacketReadInfo(ILogger logger, DataReader reader) {
            logger.LogDebug(" - Read ArcServe Section {section}/{header} at {address}. (Header End: {streamDataStart}, Data End: {alignedDataEndIndex}){extraData}", this.GetTypeDisplayName(), this.Block, reader.GetFileIndexDisplay(this.Block.HeaderStartIndex), reader.GetFileIndexDisplay(this.BodyStartIndex), reader.GetFileIndexDisplay(this.BodyEndIndex), this.GetExtraLoggerInfo());
        }
    }

    public class ArcServeUnsupportedStreamData : ArcServeStreamData
    {
        public byte[] RawData = Array.Empty<byte>();
        
        public override void LoadFromReader(DataReader reader, in ArcServeStreamHeader block) {
            base.LoadFromReader(reader, in block);
            if (block.Size > Int32.MaxValue)
                throw new DataException($"Cannot read {block.Size} bytes of stream data, it's too much.");
            
            this.RawData = reader.ReadBytes((int)block.Size);
        }
    }

    public class ArcServeStreamWindowsFileName : ArcServeStreamData
    {
        public string FileName = string.Empty;
        public string DosFileName = string.Empty;

        /// <inheritdoc cref="ArcServeStreamData.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader, in ArcServeStreamHeader block) {
            base.LoadFromReader(reader, in block);
            
            reader.SkipBytes(44);
            this.FileName = reader.ReadFixedSizeString(520, encoding: Encoding.Unicode);
            this.DosFileName = reader.ReadFixedSizeString(28, encoding: Encoding.Unicode);
        }
    }

    public class ArcServeStreamFullPathData : ArcServeStreamData
    {
        public string FullPath = string.Empty;
        
        /// <inheritdoc cref="ArcServeStreamData.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader, in ArcServeStreamHeader block) {
            base.LoadFromReader(reader, in block);
            this.FullPath = reader.ReadFixedSizeString(1024, encoding: Encoding.Unicode);
        }
    }

    public class ArcServeStreamEndData : ArcServeStreamData
    {
    }

    public class ArcServeStreamRawData : ArcServeStreamData
    {
        public byte[] RawData = Array.Empty<byte>();
        public uint ExpectedDecompressedSize;
        public byte[] UsableData = Array.Empty<byte>();
        
        /// <inheritdoc cref="ArcServeStreamData.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader, in ArcServeStreamHeader block) {
            base.LoadFromReader(reader, in block);
            if (block.Size > Int32.MaxValue)
                throw new DataException($"Cannot read {block.Size} bytes of stream data, it's too much.");
            
            if (ArcServe.FastDebuggingEnabled) {
                reader.Index += (long)block.Size;
                return;
            }

            if ((block.Flags & StreamFlags.Compressed) == StreamFlags.Compressed) {
                this.ExpectedDecompressedSize = reader.ReadUInt32();
                this.RawData = reader.ReadBytes((int)block.Size - DataConstants.IntegerSize);
                this.UsableData = TinyInflate.Uncompress(this.RawData);
            } else {
                this.UsableData = this.RawData = reader.ReadBytes((int)block.Size);
            }
        }
    }

    /// <summary>
    /// Represents raw data which is part of a catalogue.
    /// </summary>
    public class ArcServeStreamCatalogueData : ArcServeStreamRawData
    {
    }

    public enum ArcServeStreamType
    {
        DosPath = 0x1800DADA,
        FullPath = 0x1900DADA,
        File = 0x3000DADA,
        Directory = 0x3100DADA,
    }

    public struct ArcServeStreamHeader
    {
        public uint Id;
        public StreamFileSystem FileSystem;
        public ulong Size;
        public uint NameSize;
        public uint Type;
        public uint RawFlags;
        public string Name;

        public long HeaderStartIndex;
        public long HeaderEndIndex;
        
        public StreamFlags Flags => (StreamFlags)this.RawFlags;

        public const int SizeInBytes = 32;

        /// <inheritdoc cref="ValueType.ToString"/>
        public override string ToString() {
            if (string.IsNullOrWhiteSpace(this.Name)) {
                return $"{this.FileSystem.GetName()}/{this.Id:X8}/{this.Type:X8}/{this.Size}/{this.RawFlags:X}";
            } else {
                return $"'{this.Name}'/{this.FileSystem.GetName()}/{this.Id:X8}/{this.Type:X8}/{this.Size}/{this.RawFlags:X}";
            }
        }
    }

    public enum StreamFileSystem : uint
    {
        Dos = 0,
        Mac = 1,
        Windows = 5
    }

    [Flags]
    public enum StreamFlags
    {
        Unicode = DataConstants.BitFlag4,
        Compressed = DataConstants.BitFlag6,
    }
}