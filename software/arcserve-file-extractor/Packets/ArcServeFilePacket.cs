using System;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Data;

namespace OnStreamSCArcServeExtractor.Packets
{
    /// <summary>
    /// An ArcServe file packet is the name given to the highest level data structure in the tape dump.
    /// It seems ArcServe tape dumps (excluding the tape header) are just a list of file packets.
    /// </summary>
    public abstract class ArcServeFilePacket
    {
        /// <summary>
        /// The tape archive which the packet belongs to.
        /// </summary>
        public readonly ArcServeTapeArchive TapeArchive;
        
        /// <summary>
        /// The signature which identifies the packet.
        /// </summary>
        public readonly uint Signature;
        
        /// <summary>
        /// Returns whether or not the data read for this packet appears to be an intended occurrence of the packet, or if the signature was seen by coincidence in data which was not supposed to be a packet.
        /// </summary>
        public abstract bool AppearsValid { get; }

        /// <summary>
        /// The logger which can be used to write information.
        /// </summary>
        public ILogger Logger => this.TapeArchive.Logger;

        public ArcServeFilePacket(ArcServeTapeArchive tapeArchive, uint signature)
        {
            this.TapeArchive = tapeArchive;
            this.Signature = signature;
        }

        /// <summary>
        /// Loads the file packet data from the reader.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        public abstract void LoadFromReader(DataReader reader);

        /// <summary>
        /// Writes information about the packet to the logger.
        /// </summary>
        public abstract void WriteInformation();
        
        /// <summary>
        /// Process the packet after loading.
        /// </summary>
        /// <param name="reader">The reader which further data can be read from.</param>
        public abstract bool Process(DataReader reader);

        /// <summary>
        /// Creates a file packet with the given information.
        /// </summary>
        /// <param name="tapeArchive">The archive which the packet belongs to.</param>
        /// <param name="sessionHeader">The session header which holds the file packet (if the file packet is held by a session)</param>
        /// <param name="signature">The packet signature to create.</param>
        /// <returns>newFilePacket</returns>
        /// <exception cref="NullReferenceException">Thrown if the session header is null when it is necessary for it to not be null.</exception>
        public static ArcServeFilePacket? CreateFilePacketFromSignature(ArcServeTapeArchive tapeArchive, ArcServeSessionHeader? sessionHeader, uint signature)
        {
            if (signature == ArcServeFileTrailer.FileTrailerSignature) { // File ending.
                return new ArcServeFileTrailer(tapeArchive);
            } else if (Enum.IsDefined(typeof(ArcServeFileHeaderSignature), signature)) {
                if (sessionHeader == null)
                    throw new NullReferenceException("Cannot create file header without a session header!");

                ArcServeFileHeaderSignature fileHeaderSignature = (ArcServeFileHeaderSignature) signature;
                return fileHeaderSignature switch
                {
                    ArcServeFileHeaderSignature.Dos => new ArcServeFileHeaderDos(sessionHeader),
                    ArcServeFileHeaderSignature.Universal => new ArcServeFileHeaderUniversal(sessionHeader),
                    ArcServeFileHeaderSignature.WindowsNt => new ArcServeFileHeaderWindows(sessionHeader, fileHeaderSignature),
                    ArcServeFileHeaderSignature.WindowsNtWorkstation => new ArcServeFileHeaderWindows(sessionHeader, fileHeaderSignature),
                    _ => new ArcServeFileHeaderUnsupported(sessionHeader, fileHeaderSignature),
                };
            } else if (Enum.IsDefined(typeof(ArcServeSessionHeaderSignature), signature)) { // Tape header.
                return new ArcServeSessionHeader(tapeArchive, (ArcServeSessionHeaderSignature) signature);
            }

            return null;
        }
    }
}