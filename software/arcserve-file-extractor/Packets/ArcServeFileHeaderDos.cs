﻿using System;
using System.IO;
using ModToolFramework.Utils.Data;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents an ArcServe DOS file entry.
    /// The predominant file entry type seen in Andrew Borman's DDS tapes.
    /// </summary>
    public class ArcServeFileHeaderDos : ArcServeFileHeader
    {
        [ThreadStatic] private static byte[]? _fileReadBuffer;
        
        public ArcServeFileHeaderDos(ArcServeSessionHeader sessionHeader) : base(sessionHeader, ArcServeFileHeaderSignature.Dos)
        {
        }

        /// <inheritdoc cref="ArcServeFileHeader.WriteFileContents"/>
        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            _fileReadBuffer ??= new byte[2048];

            // Copy bytes from the reader directly to the writer.
            ulong bytesLeft = this.FileSizeInBytes;
            while (bytesLeft > 0) {
                int bytesRead = reader.Read(_fileReadBuffer, 0, (int) Math.Min((ulong) _fileReadBuffer.LongLength, bytesLeft));
                if (bytesRead <= 0)
                    throw new EndOfStreamException($"There's no more data to read. We read {bytesRead} byte(s), but there are {bytesLeft} byte(s) still expected to be available.");
                
                writer.Write(_fileReadBuffer, 0, bytesRead);
                bytesLeft -= (uint) bytesRead;
            }
        }
    }
}