using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;
using System;
using System.Collections.Generic;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Contains general utilities for parsing ArcServe data.
    /// </summary>
    public static class ArcServe
    {
        public const uint StreamStartSignature = 0xCAACCAACU;
        public const int RootSectorSize = 0x200;
        public const bool FastDebuggingEnabled = false;

        /// <summary>
        /// Reads the definition of a file, stopping right before the file data begins, if there is any.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="definition">Output storage for the file definition.</param>
        public static void ReadFileEntry(DataReader reader, ILogger logger, out ArcServeFileDefinition definition) {
            definition = new ArcServeFileDefinition();
            definition.RelativeFilePath = reader.ReadFixedSizeString(250);
            definition.UnknownString1 = reader.ReadFixedSizeString(33);
            definition.DirectoryLevel = reader.ReadByte();
            uint lastModificationTimestamp = reader.ReadUInt32(ByteEndian.BigEndian);
            definition.FileSizeInBytes = reader.ReadUInt32();
            reader.SkipBytes(23); // Probably safe to ignore.
            definition.LastAccessDate = reader.ReadUInt16();
            uint fileCreationTimestamp = reader.ReadUInt32(ByteEndian.LittleEndian);
            reader.SkipBytes(22);

            definition.LastModificationTime = ParseTimeStamp(lastModificationTimestamp);
            definition.FileCreationTime = ParseTimeStamp(fileCreationTimestamp);
            
            // Read data chunks.
            long sectionReadStart = reader.Index;
            ArcServeStreamData streamData;
            List<ArcServeStreamData> streamChunks = definition.StreamChunks = new List<ArcServeStreamData>();
            while ((streamData = ParseSection(reader, logger)) is not ArcServeStreamEndData) {
                if (streamData is ArcServeStreamWindowsFileName fileNamePacket)
                    definition.FileDeclaration = fileNamePacket;
                if (streamData is ArcServeStreamFullPathData fullPathPacket)
                    definition.FullPathData = fullPathPacket;
                
                streamChunks.Add(streamData);
            }

            if (!definition.IsDirectory && !definition.IsFile)
                logger.LogError($" - Expected the type at {reader.GetFileIndexDisplay(sectionReadStart)} to either be a File or a Directory, but got {definition.FileDeclaration?.Block.Type:X8} instead.");
        }
        
        /// <summary>
        /// Parses an ArcServe timestamp into <see cref="DateTime"/>.
        /// </summary>
        /// <param name="number">The ArcServe timestamp</param>
        /// <returns>Parsed timestamp</returns>
        public static DateTime ParseTimeStamp(uint number, uint startYear = 1980) {
            if (number == 0)
                return DateTime.UnixEpoch;

            uint second = (number & 0b11111) << 1;
            uint minute = (number >> 5) & 0b111111;
            uint hour = ((number >> 11) & 0b11111);
            uint day = (number >> 16) & 0b11111;
            uint month = (number >> 21) & 0b1111;
            uint year = startYear + ((number >> 25) & 0x7F);

            return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second, DateTimeKind.Local);
        }
        
        /// <summary>
        /// Reads an ArcServe data stream header from the reader, if possible.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        /// <param name="block">Output storage for the block header.</param>
        /// <returns>Whether a stream header was found/parsed.</returns>
        public static bool TryParseStreamHeader(DataReader reader, out ArcServeStreamHeader block) {
            uint magicSignature = reader.ReadUInt32(ByteEndian.BigEndian);

            block = new ArcServeStreamHeader();
            if (magicSignature != StreamStartSignature) {
                block.Id = magicSignature;
                return false;
            }

            block = new ArcServeStreamHeader();
            block.Id = reader.ReadUInt32(ByteEndian.BigEndian);
            block.FileSystem = reader.ReadEnum<uint, StreamFileSystem>();
            block.Size = reader.ReadUInt64();
            block.NameSize = reader.ReadUInt32();
            block.Type = reader.ReadUInt32(ByteEndian.BigEndian);
            block.RawFlags = reader.ReadUInt32();
            block.Name = (block.NameSize > 0) ? reader.ReadStringBytes((int)block.NameSize) : string.Empty;
            
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
            ArcServeStreamData data = ParseSection(reader, logger);
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
        public static ArcServeStreamData ParseSection(DataReader reader, ILogger logger) {
            long sectionStartIndex = reader.Index;

            if (!TryParseStreamHeader(reader, out ArcServeStreamHeader block))
                throw new Exception($"Tried to read section from {reader.GetFileIndexDisplay(sectionStartIndex)}, but we found {block.Id:X8} instead of the expected signature {StreamStartSignature:X8}.");

            long streamDataStartIndex = reader.Index;
            ArcServeStreamData packet = ArcServeStreamDataTypes.CreatePacket(in block);
            packet.LoadFromReader(reader, in block);
            long streamDataEndIndex = reader.Index;
            long readStreamDataSize = (streamDataEndIndex - streamDataStartIndex);

            // Verify correct amount was read.
            if (unchecked((ulong)readStreamDataSize) != block.Size)
                logger.LogWarning($"$ - Section @ {reader.GetFileIndexDisplay(sectionStartIndex)} had a length of {block.Size} bytes, but {readStreamDataSize} were read.");

            AlignReaderToStream(reader); // Align the reader.
            logger.LogDebug(" - Parsed ArcServe Section {section}/{header} at {address}. (Header End: {streamDataStart}, Data End : {alignedDataEndIndex})", packet.GetTypeDisplayName(), block, reader.GetFileIndexDisplay(sectionStartIndex), reader.GetFileIndexDisplay(streamDataStartIndex), reader.GetFileIndexDisplay());
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
        
        /// <summary>
        /// Test if the provided string looks valid and is probably not garbage data we read.
        /// </summary>
        /// <param name="input">The string to test.</param>
        /// <returns>Whether it looks valid</returns>
        public static bool IsValidLookingString(string? input) {
            if (string.IsNullOrWhiteSpace(input))
                return false;
            
            int validLooking = 0;
            for (int i = 0; i < input.Length; i++) {
                char temp = input[i];
                if ((temp >= 'a' && temp <= 'z') || (temp >= 'A' && temp <= 'Z') || (temp >= '0' && temp <= '9') || temp == '\\' || temp == '_' || temp == '.' || temp == '~' || temp == '-')
                    validLooking++;
            }

            return input.Length < 20 || validLooking >= input.Length / 2;
        }
    }
    
    /// <summary>
    /// Represents the definition of a file.
    /// </summary>
    public struct ArcServeFileDefinition
    {
        public string RelativeFilePath; // Relative to the root directory. Seems to be a Dos Path on Windows.
        public string UnknownString1;
        public byte DirectoryLevel;
        public DateTime LastModificationTime;
        public uint FileSizeInBytes;
        public ushort LastAccessDate;
        public DateTime FileCreationTime;
        public ArcServeStreamWindowsFileName? FileDeclaration;
        public ArcServeStreamFullPathData? FullPathData;
        public List<ArcServeStreamData>? StreamChunks;

        public bool IsFile => this.FileDeclaration == null || this.FileDeclaration.Block.Type == (uint)ArcServeStreamType.File;
        public bool IsDirectory => this.FileDeclaration != null && this.FileDeclaration.Block.Type == (uint)ArcServeStreamType.Directory;
        public string FullPath => this.FullPathData?.FullPath ?? this.RelativeFilePath;
    }
}