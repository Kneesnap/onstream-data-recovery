using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents a universal file entry.
    /// Seems to be the most common file entry type.
    /// Seen in Frogger EOP, Chicken Run EOP Tape #2, and very briefly in Andrew Borman's tape.
    /// TODO: File name chunks should impact the file name in the original.
    /// TODO: Redo our logic for managing sections too.
    /// </summary>
    public class ArcServeFileHeaderUniversal : ArcServeFileHeader
    {
        public ArcServeStreamWindowsFileName? FileDeclaration { get; private set; }
        public ArcServeStreamFullPathData? FullPathData { get; private set; }
        public override bool IsDirectory => base.IsDirectory || (this.FileDeclaration != null && this.FileDeclaration.Block.Type == (uint) ArcServeStreamType.Directory);
        
        public ArcServeFileHeaderUniversal(ArcServeSessionHeader sessionHeader) : base(sessionHeader, ArcServeFileHeaderSignature.Universal)
        {
        }
        
        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            // Process file data.
            uint sectionId = 0;
            foreach (ArcServeStreamData streamDataChunk in this.ReadDataChunksFromReader(reader)) {
                if (streamDataChunk is not ArcServeStreamRawData rawData)
                    continue;

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
            if (this.CachedDataChunkStream != null)
                throw new ApplicationException("The data chunks have already been read, so the cache should be used instead.");

            // Create the cache.
            if (storeInCache)
                this.CachedDataChunkStream = new List<ArcServeStreamData>();
            
            // Read data chunks.
            long dataStreamStartIndex = reader.Index;
            long lastChunkStartIndex = dataStreamStartIndex;
            ArcServeStreamData streamData;
            while ((streamData = ParseSectionSafe(reader, lastChunkStartIndex)) is not ArcServeStreamEndData) {
                if (streamData is ArcServeStreamWindowsFileName fileNamePacket)
                    this.FileDeclaration = fileNamePacket;
                if (streamData is ArcServeStreamFullPathData fullPathPacket)
                    this.FullPathData = fullPathPacket;

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
                return ArcServe.ParseSection(reader, this.Logger);
            } catch (Exception ex) {
                throw new DataException($"Failed when parsing stream chunk at {reader.GetFileIndexDisplay(lastChunkStartIndex)}", ex);
            }
        }
    }
}