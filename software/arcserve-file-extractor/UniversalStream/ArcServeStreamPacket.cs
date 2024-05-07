using System;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.UniversalStream
{
    /// <summary>
    /// Represents a packet of data in a universal file entry.
    /// TODO: Make this the base class.
    /// TODO: The data packet system needs to support loading without the header. For example, it seems like the File header info seen in Chicken Run file headers is the same data seen in stream chunks.
    ///  - The primary difference seems to be that one the packet ordering is implicitly known.
    ///  - This means we'll be able to share re-use chunk logic across different file headers.
    /// </summary>
    public static class ArcServeStreamPacket
    {
        public const uint StreamStartSignature = 0xCAACCAACU;

        /// <summary>
        /// Reads an ArcServe data stream header from the reader, if possible.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        /// <param name="signature">Output storage for the stream signature, regardless of if it was valid.</param>
        /// <param name="block">Output storage for the block header.</param>
        /// <returns>Whether a stream header was found/parsed.</returns>
        public static bool TryParseStreamHeader(DataReader reader, out uint signature, out ArcServeStreamHeader block) {
            long readerStartIndex = reader.Index;
            signature = reader.ReadUInt32(ByteEndian.BigEndian);

            block = new ArcServeStreamHeader();
            block.HeaderStartIndex = readerStartIndex;
            if (signature != StreamStartSignature) {
                reader.Index = readerStartIndex;
                return false;
            }

            block.Id = reader.ReadUInt32(ByteEndian.BigEndian);
            block.FileSystem = reader.ReadEnum<uint, StreamFileSystem>();
            block.Size = reader.ReadUInt64();
            block.NameSize = reader.ReadUInt32();
            block.Type = reader.ReadUInt32(ByteEndian.BigEndian);
            block.RawFlags = reader.ReadUInt32();

            if (block.NameSize > 0) {
                block.Name = reader.ReadStringBytes((int) block.NameSize - 1);
                reader.SkipBytesRequireEmpty(1);
            } else {
                block.Name = string.Empty;
            }

            block.HeaderEndIndex = reader.Index;
            return true;
        }
        
        /// <summary>
        /// Parses the next stream section (header + data) and requires that it is a certain type.
        /// If it is not of the correct type, or no stream could be read, an exception will be thrown.
        /// </summary>
        /// <param name="reader">The source to read data from.</param>
        /// <param name="logger">The logger to log information to.</param>
        /// <typeparam name="TStreamData">The type of stream section which is required.</typeparam>
        /// <returns>section</returns>
        /// <exception cref="InvalidCastException">Thrown if the wrong section was found.</exception>
        public static TStreamData RequireSection<TStreamData>(DataReader reader, ILogger logger) where TStreamData : ArcServeStreamData, new() {
            long startReadIndex = reader.Index;
            ArcServeStreamData data = ReadStreamPacket(reader, logger);
            if (data is TStreamData typedData)
                return typedData;

            throw new InvalidCastException($"Expected an {typeof(TStreamData).GetDisplayName()} section at {reader.GetFileIndexDisplay(startReadIndex)}, but got {data.GetTypeDisplayName()}/{data.Block} section instead.");
        }

        /// <summary>
        /// Parses the next available data as a stream section, parsing both the header and the data.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <returns>parsed stream section</returns>
        /// <exception cref="Exception">Thrown if there was no stream to read, or if there was an error reading the stream.</exception>
        public static ArcServeStreamData ReadStreamPacket(DataReader reader, ILogger logger) {
            long headerStartIndex = reader.Index;

            if (!TryParseStreamHeader(reader, out uint magicSignature, out ArcServeStreamHeader streamHeader))
                throw new Exception($"Tried to read section from {reader.GetFileIndexDisplay(headerStartIndex)}, but we found {magicSignature:X8} instead of the expected signature {StreamStartSignature:X8}.");

            return ReadStreamPacketWithHeader(reader, logger, headerStartIndex, in streamHeader);
        }

        /// <summary>
        /// Parses the next available data as a stream section, with an already parsed header.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="headerStartIndex">The index which the header data began.</param>
        /// <param name="streamHeader">The header .</param>
        /// <returns>parsed stream section</returns>
        /// <exception cref="Exception">Thrown if there was no stream to read, or if there was an error reading the stream.</exception>
        public static ArcServeStreamData ReadStreamPacketWithHeader(DataReader reader, ILogger logger, long headerStartIndex, in ArcServeStreamHeader streamHeader) {
            ArcServeStreamData packet = ArcServeStreamDataTypes.CreatePacket(in streamHeader);
            long streamDataStartIndex = packet.BodyStartIndex = reader.Index;
            packet.LoadFromReader(reader, in streamHeader);
            long streamDataEndIndex = packet.BodyEndIndex = reader.Index;
            long readStreamDataSize = (streamDataEndIndex - streamDataStartIndex);

            // Verify correct amount was read.
            if (unchecked((ulong)readStreamDataSize) != streamHeader.Size)
                logger.LogWarning(" - Stream Header @ {headerStartIndex} had a length of {blockSize} bytes, but {readStreamDataSize} were read.", reader.GetFileIndexDisplay(headerStartIndex), streamHeader.Size, readStreamDataSize);

            AlignReaderToStream(reader); // Align the reader.
            return packet;
        }


        /// <summary>
        /// Aligns the reader to the next position which a stream section may be.
        /// </summary>
        /// <param name="reader">The reader to align.</param>
        public static void AlignReaderToStream(DataReader reader) {
            // Align to the next available 3rd byte, for some reason...
            // If for some reason this stops working at some point, the reason it has failed is probably that it was wrong to assume it was the 3rd byte it should be aligned to.
            // In that situation, it should probably be aligned to the same alignment as the previous stream section header.
            
            int remainder = (int)(reader.Index % 4);
            reader.SkipBytes(3 - remainder);
        }
    }
}