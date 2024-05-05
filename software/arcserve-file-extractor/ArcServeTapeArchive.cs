using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using OnStreamSCArcServeExtractor.Packets;
using OnStreamTapeLibrary;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Represents an ArcServe tape archive.
    /// An archive represents the relation between raw tape dumps, their parsed representations, and the output file archive.
    /// What ties these things together is a single output.
    /// </summary>
    public class ArcServeTapeArchive
    {
        /// <summary>
        /// Contains the configuration of a tape.
        /// </summary>
        public readonly TapeDefinition Definition;

        /// <summary>
        /// The logger to use for logging information.
        /// </summary>
        public readonly ILogger Logger;

        /// <summary>
        /// The .zip archive which files are written to.
        /// </summary>
        public readonly ZipArchive Archive;

        /// <summary>
        /// The current root path in the output archive to place files at.
        /// </summary>
        public string? CurrentBasePath;
        
        /// <summary>
        /// The parsed file packets in read order.
        /// </summary>
        public readonly List<ArcServeFilePacket> OrderedPackets = new ();

        /// <summary>
        /// Creates a new <see cref="ArcServeTapeArchive"/>.
        /// </summary>
        /// <param name="definition">The tape definition use for this archive.</param>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="outputArchive">The output archive to save files to.</param>
        public ArcServeTapeArchive(TapeDefinition definition, ILogger logger, ZipArchive outputArchive)
        {
            this.Definition = definition;
            this.Logger = logger;
            this.Archive = outputArchive;
        }
    }
}