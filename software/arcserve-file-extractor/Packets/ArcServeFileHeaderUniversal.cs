using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using OnStreamSCArcServeExtractor.UniversalStream;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents a universal file entry.
    /// Seems to be the most common file entry type.
    /// Seen in Frogger EOP, Chicken Run EOP Tape #2, and very briefly in Andrew Borman's tape.
    /// </summary>
    public class ArcServeFileHeaderUniversal : ArcServeFileHeader
    {
        public ArcServeStreamWindowsFileName? FileDeclaration { get; private set; }
        public ArcServeStreamFullPathData? FullPathData { get; private set; }
        public override bool IsDirectory => base.IsDirectory || (this.FileDeclaration != null && this.FileDeclaration.Block.Type == (uint) ArcServeStreamType.Directory);
        public override string RelativeFilePath => this.FullPathData?.FullPath ?? base.RelativeFilePath;
        
        public ArcServeFileHeaderUniversal(ArcServeSessionHeader sessionHeader) : base(sessionHeader, ArcServeFileHeaderSignature.Universal)
        {
        }

        /// <inheritdoc cref="ArcServeFilePacket.LoadFromReader"/>
        public override void LoadFromReader(DataReader reader)
        {
            base.LoadFromReader(reader);

            // Read chunks excluding file data, since that will be parsed later.
            this.CachedDataChunkStream ??= new List<ArcServeStreamData>();
            while (reader.HasMore) {
                long headerStartIndex = reader.Index;
                bool readHeader = ArcServeStreamPacket.TryParseStreamHeader(reader, out _, out ArcServeStreamHeader streamHeader);

                // Attempt to read the stream packet. If it fails, we're done.
                if (readHeader && ((streamHeader.Id == 0x2110DAAD) || (streamHeader.Id == 0x1000DAAD && streamHeader.Type == 0x1900DADA) || (streamHeader.Id == 0x2310DAAD))) {
                    try {
                        ArcServeStreamData streamPacket = ArcServeStreamPacket.ReadStreamPacketWithHeader(reader, this.Logger, headerStartIndex, in streamHeader);
                        if (streamPacket is ArcServeStreamWindowsFileName fileNamePacket)
                            this.FileDeclaration = fileNamePacket;
                        if (streamPacket is ArcServeStreamFullPathData fullPathPacket)
                            this.FullPathData = fullPathPacket;
                        
                        this.CachedDataChunkStream.Add(streamPacket);
                        continue; // Ensure the loop continues.
                    } catch {
                        // If it fails, this will be read again later, it should be safe to silently fail.
                    }
                }
                
                // Restore the reader to before the start index.
                reader.Index = headerStartIndex;
                break;
            }
        }

        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void ReadAndDisplayExtraFileData(DataReader reader) {
            base.ReadAndDisplayExtraFileData(reader);
            
            // Process remaining chunks.
            foreach (ArcServeStreamData streamDataChunk in this.ReadDataChunksFromReader(reader, true)) {
                streamDataChunk.WritePacketReadInfo(this.Logger, reader);
            }
        }

        /// <inheritdoc cref="ArcServeFilePacket.Process"/>
        public override bool Process(DataReader reader)
        {
            if (ArcServe.FastDebuggingEnabled)
                return true; // TODO: We should be skipping the correct number of chunks / bytes instead of just flat skipping.

            return base.Process(reader);
        }

        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            // Process file data.
            uint sectionId = 0;
            foreach (ArcServeStreamData streamDataChunk in this.ReadDataChunksFromReader(reader)) {
                if (streamDataChunk is not ArcServeStreamRawData rawData) {
                    this.CachedDataChunkStream?.Add(streamDataChunk); // Cache everything but the raw file data.
                    streamDataChunk.WritePacketReadInfo(this.Logger, reader);
                    continue;
                }

                long tempStartIndex = reader.Index;
                if (rawData.ExpectedDecompressedSize != 0 && rawData.RawData != rawData.UsableData && rawData.UsableData.Length != rawData.ExpectedDecompressedSize)
                    this.Logger.LogWarning(" - Read failure! Section {sectionId} (At {tempStartIndex}) was expected to decompress to {rawDataExpectedDecompressedSize} bytes, but actually decompressed to {rawDataUsableLength} bytes.", sectionId, reader.GetFileIndexDisplay(tempStartIndex), rawData.ExpectedDecompressedSize, rawData.UsableData.Length);

                writer.Write(rawData.UsableData);
                sectionId++;
            }
        }

        /// <summary>
        /// Read data chunks from the reader.
        /// Assumes the reader is at the correct position.
        /// </summary>
        /// <param name="reader">The reader to read data chunks from.</param>
        /// <param name="storeInCache">If true, the chunks will be saved in a cache.</param>
        /// <returns>dataChunks</returns>
        public IEnumerable<ArcServeStreamData> ReadDataChunksFromReader(DataReader reader, bool storeInCache = false) {
            // Create the cache.
            if (storeInCache)
                this.CachedDataChunkStream = new List<ArcServeStreamData>();
            
            // Read data chunks.
            long dataStreamStartIndex = reader.Index;
            long lastChunkStartIndex = dataStreamStartIndex;
            ArcServeStreamData? streamData = null;
            while (streamData is not ArcServeStreamEndData && (streamData = ParseSectionSafe(reader, lastChunkStartIndex)) != null) {
                if (storeInCache)
                    this.CachedDataChunkStream?.Add(streamData);
                lastChunkStartIndex = reader.Index;
                yield return streamData;
            }
        }

        /// <summary>
        /// This function purely exists to avoid a try {} catch {}'s incompatibility with yield return.
        /// </summary>
        /// <returns></returns>
        private ArcServeStreamData ParseSectionSafe(DataReader reader, long lastChunkStartIndex) {
            try {
                return ArcServeStreamPacket.ReadStreamPacket(reader, this.Logger);
            } catch (Exception ex) {
                throw new DataException($"Failed when parsing stream chunk at {reader.GetFileIndexDisplay(lastChunkStartIndex)}", ex);
            }
        }
    }
}