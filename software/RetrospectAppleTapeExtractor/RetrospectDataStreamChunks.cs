using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using OnStreamTapeLibrary;
using RetrospectTape.MacEncoding;
using System;
using System.Collections.Generic;
using System.Data;

namespace RetrospectTape
{
    /// <summary>
    /// A registration of Retrospect data chunks and utilities.
    /// </summary>
    public static class RetrospectDataStreamChunks
    {
        private static readonly Dictionary<uint, Type> ChunkTypes = new Dictionary<uint, Type>();

        private static void RegisterChunkType<TChunk>(ReadOnlySpan<char> marker) 
            where TChunk : RetrospectDataStreamChunk, new() {
            if (marker.Length != 4)
                throw new DataException($"Marker is expected to have four characters but has {marker.Length}.");

            uint signature = 0;
            signature |= (uint)(((byte)marker[0]) << 24);
            signature |= (uint)(((byte)marker[1]) << 16);
            signature |= (uint)(((byte)marker[2]) << 8);
            signature |= ((byte)marker[3]);
            
            ChunkTypes[signature] = typeof(TChunk);
        }

        static RetrospectDataStreamChunks() {
            RegisterChunkType<RetrospectSegmentChunk>(RetrospectSegmentChunk.Signature);
            RegisterChunkType<RetrospectPrivateChunk>(RetrospectPrivateChunk.Signature);
            RegisterChunkType<RetrospectFileChunk>(RetrospectFileChunk.Signature);
            RegisterChunkType<RetrospectForkChunk>(RetrospectForkChunk.Signature);
            RegisterChunkType<RetrospectContinueChunk>(RetrospectContinueChunk.Signature);
            RegisterChunkType<RetrospectTailChunk>(RetrospectTailChunk.Signature);
            RegisterChunkType<RetrospectDirectoryChunk>(RetrospectDirectoryChunk.Signature);
            RegisterChunkType<RetrospectSegmentNodX>(RetrospectSegmentNodX.Signature);
            RegisterChunkType<RetrospectSnapshotChunk>(RetrospectSnapshotChunk.Signature);
        }
        
        /// <summary>
        /// Creates a new streaming data chunk from the signature.
        /// </summary>
        /// <param name="signature">The signature to create a chunk from.</param>
        /// <param name="chunk">Output storage for the new chunk</param>
        /// <returns>Whether a chunk was created from the signature.</returns>
        /// <exception cref="InvalidCastException">Thrown if the registered data type is invalid.</exception>
        public static bool TryCreateChunk(uint signature, out RetrospectDataStreamChunk? chunk) {
            if (!ChunkTypes.TryGetValue(signature, out Type? chunkType)) {
                chunk = null;
                return false;
            }

            object? newObject = Activator.CreateInstance(chunkType);
            if (newObject is RetrospectDataStreamChunk newChunk) {
                chunk = newChunk;
                return true;
            }

            throw new InvalidCastException($"The type {chunkType} is not an ArcServe packet type!");
        }
        
        /// <summary>
        /// Attempts to parse the next available data from the reader as a stream chunk.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="output">The output storage for the new chunk.</param>
        /// <returns>Whether a chunk was parsed successfully</returns>
        /// <exception cref="Exception">Thrown if there was no stream to read, or if there was an error reading the stream.</exception>
        public static bool TryParseChunk(DataReader reader, ILogger logger, out RetrospectDataStreamChunk? output) {
            if (reader.Remaining < (2 * DataConstants.IntegerSize)) {
                output = null;
                return false;
            }
            
            long startIndex = reader.Index;

            uint signature = reader.ReadUInt32();
            if (!TryCreateChunk(signature, out RetrospectDataStreamChunk? chunk) || chunk == null) {
                reader.Index = startIndex;
                output = chunk;
                return false;
            }

            if (!chunk.TrySetup(reader, logger)) {
                reader.Index = startIndex;
                output = chunk;
                return false;
            }

            output = chunk;
            return true;
        }
    }
    
    /// <summary>
    /// The basic representation of a Retrospect data chunk.
    /// </summary>
    public abstract class RetrospectDataStreamChunk
    {
        /// <summary>
        /// Whether the chunk should be logged or not.
        /// </summary>
        public virtual bool ShouldLog => true;
        
        /// <summary>
        /// 
        /// </summary>
        public virtual bool ShouldRemember => false;

        /// <summary>
        /// The ID to remember this chunk by.
        /// </summary>
        /// <exception cref="Exception">Thrown if it's not overriden.</exception>
        public virtual uint RememberId => throw new Exception($"{nameof(this.RememberId)} should be overriden by {this.GetTypeDisplayName()}.");

        /// <summary>
        /// Reads chunk-specific data from the reader.
        /// </summary>
        /// <param name="reader">The reader to read data from</param>
        /// <param name="logger">The logger to log information to</param>
        /// <param name="dataAvailable">The amount of bytes which should be read.</param>
        /// <returns>Whether the data was read successfully.</returns>
        protected abstract bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable);
        
        /// <summary>
        /// Attempts to setup the stream chunk by loading data from a reader.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        /// <param name="logger">The logger to write output information to.</param>
        /// <returns>Whether the section was setup properly.</returns>
        public bool TrySetup(DataReader reader, ILogger logger) {
            long startIndex = reader.Index - DataConstants.IntegerSize;
            uint segmentLength = reader.ReadUInt32();
            uint segmentLengthRemaining = segmentLength - (2 * DataConstants.IntegerSize);

            if (segmentLengthRemaining > reader.Remaining) {
                logger.LogWarning($" - Section @ {reader.GetFileIndexDisplay(startIndex)} had a length of {segmentLengthRemaining}, but only {reader.Remaining} bytes are available.");
                return false;
            }
            
            // Load data.
            bool readResult;
            try {
                readResult = this.LoadFromReader(reader, logger, segmentLengthRemaining);
            } catch (Exception ex) {
                logger.LogError($" - Encountered an error reading section @ {reader.GetFileIndexDisplay(startIndex)}, {this.GetTypeDisplayName()}\n{ex}");
                return false;
            }

            if (!readResult) {
                logger.LogWarning($" - Section @ {reader.GetFileIndexDisplay(startIndex)} failed to read as a(n) {this.GetTypeDisplayName()}.");
                return false;
            }
            
            // Verify correct amount was read.
            long readStreamDataSize = (reader.Index - startIndex);
            if (readStreamDataSize != segmentLength) {
                logger.LogWarning($" - Section @ {reader.GetFileIndexDisplay(startIndex)} had a length of {segmentLength} bytes, but {readStreamDataSize} bytes were actually read.");
                return false;
            }

            return true;
        }
    }

    public class RetrospectSegmentChunk : RetrospectDataStreamChunk
    {
        public const string Signature = "Sgmt";

        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            reader.Index += dataAvailable;
            return true;
        }
    }

    public class RetrospectSegmentNodX : RetrospectDataStreamChunk
    {
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldLog"/>
        public override bool ShouldLog => false; // Seems there's always one before a directory, but it doesn't seem like it has anything we care about?.
        
        public const string Signature = "NodX";

        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            reader.Index += dataAvailable;
            return true;
        }
    }
    
    public class RetrospectSnapshotChunk : RetrospectDataStreamChunk
    {
        public ushort Unknown1 { get; private set; }
        public DateTime BackupTime { get; private set; }
        public uint Unknown2 { get; private set; }
        public string FinderType { get; private set; } = string.Empty;
        public ushort Unknown3 { get; private set; }
        public string ParentFolderName { get; private set; } = string.Empty;
        public string FolderName { get; private set; } = string.Empty;
        public uint FileSize { get; private set; }
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldRemember"/>
        public override bool ShouldRemember => true;
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.RememberId"/>
        public override uint RememberId => this.Unknown1;


        public const string Signature = "Snap";

        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            this.Unknown1 = reader.ReadUInt16();
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime date);
            this.BackupTime = date;
            this.Unknown2 = reader.ReadUInt32();
            this.FinderType = reader.ReadStringBytes(4, MacRoman.Instance);
            this.Unknown3 = reader.ReadUInt16();
            this.ParentFolderName = reader.ReadFixedSizeString(32, encoding: MacRoman.Instance);
            this.FolderName = reader.ReadFixedSizeString(32, encoding: MacRoman.Instance);
            this.FileSize = reader.ReadUInt32();

            uint zero1 = reader.ReadUInt32();
            if (zero1 != 0)
                throw new Exception($"Expected value to be zero in snapshot chunk, but was {zero1}.");
            
            uint zero2 = reader.ReadUInt32();
            if (zero2 != 0)
                throw new Exception($"Expected value to be zero in snapshot chunk, but was {zero2}.");
            
            return true;
        }
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"Snapshot: {this.Unknown1}/{this.Unknown1:X4}, {this.Unknown2:X8}, {this.Unknown3}, Backup Time: {this.BackupTime}, Type: '{this.FinderType}', Name: '{this.ParentFolderName}', '{this.FolderName}', File Size: {this.FileSize}/{DataUtils.ConvertByteCountToFileSize(this.FileSize)}";
        }
    }
    
    public class RetrospectPrivateChunk : RetrospectDataStreamChunk
    {
        public ushort Unknown1 { get; private set; }
        public uint Unknown2 { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public const string Signature = "Priv";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            this.Unknown1 = reader.ReadUInt16();
            this.Unknown2 = reader.ReadUInt32();

            uint zero1 = reader.ReadUInt32();
            if (zero1 != 0)
                throw new Exception($"Expected zero, but read {zero1} from {reader.GetFileIndexDisplay()}.");
            
            uint zero2 = reader.ReadUInt32();
            if (zero2 != 0)
                throw new Exception($"Expected zero, but read {zero2} from {reader.GetFileIndexDisplay()}.");
            
            this.Name = reader.ReadNullTerminatedString(MacRoman.Instance);
            return true;
        }
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"Private: {this.Name}, {this.Unknown1}, {this.Unknown2}";
        }
    }

    /// <summary>
    /// Represents a file chunk.
    /// This struct referenced in the comments below is 'FndrFileInfo' from https://opensource.apple.com/source/hfs/hfs-366.1.1/core/hfs_format.h.auto.html
    /// </summary>
    public class RetrospectFileChunk : RetrospectDataStreamChunk
    {
        public uint ResourceId { get; private set; } // This ID is seen in all subsequent forks, and the 'Tail'.
        public uint FolderId { get; private set; }
        public DateTime BackupTime { get; private set; }
        public DateTime CreationTime { get; private set; }
        public DateTime LastModified { get; private set; }
        public uint FileSize1 { get; private set; }
        public uint FileSize2 { get; private set; }
        public string FileType { get; private set; } = string.Empty; // This is tracked by finder as 'fdType'.
        public string SoftwareThatCreatedTheFile { get; private set; } = string.Empty; // This is tracked by finder as 'fdCreator'
        public string FileName { get; private set; } = string.Empty;

        public uint FileSize => this.FileSize1 + this.FileSize2;
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldRemember"/>
        public override bool ShouldRemember => true;

        /// <inheritdoc cref="RetrospectDataStreamChunk.RememberId"/>
        public override uint RememberId => this.ResourceId;
        
        public const string Signature = "File";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime backupTime);
            reader.SkipBytes(2); // 2 empty bytes?
            this.ResourceId = reader.ReadUInt32();
            this.FolderId = reader.ReadUInt32();
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime creationTime);
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime lastModified);

            this.BackupTime = backupTime;
            this.CreationTime = creationTime;
            this.LastModified = lastModified;

            this.FileSize1 = reader.ReadUInt32(); // For some unknown reason it's split up into two numbers.
            this.FileSize2 = reader.ReadUInt32();
            this.FileType = reader.ReadStringBytes(4, MacRoman.Instance);
            this.SoftwareThatCreatedTheFile = reader.ReadStringBytes(4, MacRoman.Instance);
            reader.SkipBytes(24); // These are likely file attributes.
            this.FileName = reader.ReadNullTerminatedString(MacRoman.Instance);

            byte zero = reader.ReadByte();
            if (zero != 0)
                throw new Exception($"Expected a trailing zero after the file name, but got {zero:X2}.");
            
            return true;
        }
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"[{this.ResourceId:X8}/{this.FolderId:X8}/{this.FileType}/{this.SoftwareThatCreatedTheFile}] File: '{this.FileName}', {this.FileSize1}/{DataUtils.ConvertByteCountToFileSize(this.FileSize1)}, {this.FileSize2}/{DataUtils.ConvertByteCountToFileSize(this.FileSize2)}, Creation: {this.CreationTime}, Last Modified: {this.LastModified}, Backup Time: {this.BackupTime}";
        }
    }

    public class RetrospectForkChunk : RetrospectDataStreamChunk
    {
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldLog"/>
        public override bool ShouldLog => false;
        
        public uint ResourceId { get; private set; } // This seems to match the value seen in the file declaration.
        public ushort FileChunkIndex { get; private set; } // Seems to count the number of chunks since the start of the file, and this seems to suggest that forks occur every 32 continue blocks. It is unknown what happens if this value were to overflow.
        public bool LastForkInFile { get; private set; }
        public byte[] Data { get; private set; } = Array.Empty<byte>();
        
        public const string Signature = "Fork";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            this.ResourceId = reader.ReadUInt32();
            dataAvailable -= DataConstants.IntegerSize;
            this.FileChunkIndex = reader.ReadUInt16();
            dataAvailable -= DataConstants.ShortSize;
            uint lastForkForFile = reader.ReadUInt32();
            dataAvailable -= DataConstants.IntegerSize;

            if (lastForkForFile > 1)
                throw new Exception($"Expected {lastForkForFile} to be either 0 or 1 (boolean), but was {lastForkForFile}.");
            this.LastForkInFile = (lastForkForFile == 1);

            for (int i = 0; i < 3; i++) {
                uint zero = reader.ReadUInt32();
                dataAvailable -= DataConstants.IntegerSize;
                if (zero != 0)
                    throw new Exception($"Value {i} was expected to be zero! (Was: {zero:X8})");
            }
            
            this.Data = reader.ReadBytes((int)dataAvailable);
            return true;
        }
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"Fork, FileID: {this.ResourceId:X8}, Data: {this.Data.Length} bytes, Chunk Pos: {this.FileChunkIndex}, Last Fork in File: {this.LastForkInFile}";
        }
    }

    public class RetrospectContinueChunk : RetrospectDataStreamChunk
    {
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldLog"/>
        public override bool ShouldLog => false;

        public byte[] Data { get; private set; } = Array.Empty<byte>();
        
        public const string Signature = "Cont";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            this.Data = reader.ReadBytes((int)dataAvailable);
            return true;
        }
        
        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"{this.Data.Length} bytes in section.";
        }
    }

    public class RetrospectTailChunk : RetrospectDataStreamChunk
    {
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldLog"/>
        public override bool ShouldLog => false;
        
        public DateTime BackupTime { get; private set; }
        public uint ResourceId { get; private set; } // Same as the ID seen at the file start.
        public const string Signature = "Tail";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime backupTime);
            this.BackupTime = backupTime;
            this.ResourceId = reader.ReadUInt32();

            uint zero1 = reader.ReadUInt32();
            if (zero1 != 0)
                throw new Exception($"TAIL VALUE #1 EXPECTED TO BE ZERO WAS {zero1}.");
            
            uint zero2 = reader.ReadUInt32();
            if (zero2 != 0)
                throw new Exception($"TAIL VALUE #2 EXPECTED TO BE ZERO WAS {zero2}.");
            
            return true;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"Tail, ID: {this.ResourceId:X8}, Backup Time: {this.BackupTime}";
        }
    }
    
    public class RetrospectDirectoryChunk : RetrospectDataStreamChunk
    {
        public DateTime CreationTime { get; private set; }
        public DateTime LastModified { get; private set; }
        public DateTime BackupTime { get; private set; }

        public uint ResourceId { get; private set; } // Same as the ID seen at the file start.
        public uint ParentId { get; private set; } // ID of the parent chunk.
        public string FolderName { get; private set; } = string.Empty;
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.ShouldRemember"/>
        public override bool ShouldRemember => true;
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.RememberId"/>
        public override uint RememberId => this.ResourceId;
        
        public const string Signature = "Diry";
        
        /// <inheritdoc cref="RetrospectDataStreamChunk.LoadFromReader"/>
        protected override bool LoadFromReader(DataReader reader, ILogger logger, uint dataAvailable) {
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime backupTime);
            reader.SkipBytes(2); // 2 empty bytes?
            this.ResourceId = reader.ReadUInt32();
            this.ParentId = reader.ReadUInt32();
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime creationTime);
            RetrospectTapeExtractor.ParseAppleDate(reader.ReadUInt32(), out DateTime lastModified);

            this.BackupTime = backupTime;
            this.CreationTime = creationTime;
            this.LastModified = lastModified;
            reader.SkipBytes(50); // This is probably just stuff like attributes.
            this.FolderName = reader.ReadNullTerminatedString(MacRoman.Instance);
            
            byte zero = reader.ReadByte();
            if (zero != 0)
                throw new Exception($"Expected a trailing zero after the folder name, but got {zero:X2}.");
            
            return true;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"[{this.ResourceId:X8}/{this.ParentId:X8}] Folder: '{this.FolderName}', Creation: {this.CreationTime}, Last Modification: {this.LastModified}, Backup Time: {this.BackupTime}";
        }
    }
}