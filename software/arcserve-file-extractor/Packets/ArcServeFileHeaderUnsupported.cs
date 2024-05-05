using System.IO;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// Represents an unsupported file entry header.
    /// </summary>
    public class ArcServeFileHeaderUnsupported : ArcServeFileHeader
    {
        public ArcServeFileHeaderUnsupported(ArcServeSessionHeader sessionHeader, ArcServeFileHeaderSignature signature) : base(sessionHeader, signature)
        {
        }

        protected override void WriteFileContents(DataReader reader, Stream writer)
        {
            this.Logger.LogError(" - This file header is not supported, and support will need to be coded to work properly.");
        }
    }
}