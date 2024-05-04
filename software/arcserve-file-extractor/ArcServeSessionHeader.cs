using System;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;
using ModToolFramework.Utils.Extensions;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Represents the header of an ArcServe tape session.
    /// </summary>
    public struct ArcServeSessionHeader {
        public ArcServeSessionHeaderSignature Signature;
        public string? RootDirectoryPath;
        public string? UserName;
        public string? Password;
        public string? Description;
        public ArcServeSessionType Type;
        public ArcServeSessionMode Mode;
        public ArcServeSessionFlags Flags;
        public ArcServeCompressionType CompressionType;
        public byte CompressionLevel;
        public byte UnixFileSystemNameLength;
        public byte LastSession;
        public ushort ExtendedSessionHeader;
        
        // Encryption
        public byte SizeOfSizeOfEncryptedEncryptionPasswordKey;
        public byte SizeOfEncryptedEncryptionBabKey;
        public byte[] SecondPartOfEncryptedEncryptionPasswordKey;
        public byte[] SecondPartOfEncryptedEncryptionBabKey;
        public byte[] EncryptionKey;

        public uint Version;
        public ushort TapeNumber;
        public DateTime? StartTime;
        public ArcServeWorkStationType WorkStationType;
        public string? WorkStationName;
        
        // Os2:
        public ArcServeCompressionMethod CompressionMethod;
        public ushort BackupDateOs2;
        public ushort BackupTimeOs2;
        public byte[] IndexFileOs2;

        /// <summary>
        /// Prints session header information to the logger.
        /// </summary>
        /// <param name="logger">The logger to write session header information to</param>
        /// <param name="padding">The padding to apply to the left of the information.</param>
        public void PrintSessionHeaderInformation(ILogger logger, string padding = "") {
            logger.LogInformation("{padding}ArcServe Tape Session:", padding);
            logger.LogInformation("{padding} Root Directory Path: '{rootDirectoryPath}'", padding, this.RootDirectoryPath);
            logger.LogInformation("{padding} Description: '{description}'", padding, this.Description);
            if (this.Version != 0)
                logger.LogInformation("{padding} App Version: {version:X8}", padding, this.Version);

            // Session Info:
            logger.LogInformation("{padding} Session Signature: {signature}", padding, this.Signature.GetName());
            logger.LogInformation("{padding} Session Start Time: {startTime}", padding, this.StartTime);
            logger.LogInformation("{padding} Session Type: {type}", padding, this.Type.GetName());
            logger.LogInformation("{padding} Session Mode: {mode}", padding, this.Mode.GetName());
            logger.LogInformation("{padding} Session Flags: {flags}", padding, this.Flags.CalculateName());
            logger.LogInformation("{padding} Username: '{userName}'", padding, this.UserName);
            logger.LogInformation("{padding} Password: '{password}'", padding, this.Password);
            logger.LogInformation("{padding} Tape Number: {tapeNumber}", padding, this.TapeNumber);
            logger.LogInformation("{padding} WorkStation Type: {workStationType}", padding, this.WorkStationType.GetName());
            if (!string.IsNullOrEmpty(this.WorkStationName)) 
                logger.LogInformation("{padding} WorkStation Name: {workStationName}", padding, this.WorkStationName);

            // Compression
            logger.LogInformation("{padding} Compression Type: {compressionType}", padding, this.CompressionType.GetName());
            logger.LogInformation("{padding} Compression Level: {compressionLevel}", padding, this.CompressionLevel);
            logger.LogInformation("{padding} Compression Method: {compressionMethod}", padding, this.CompressionMethod.GetName());

            // Encryption
            if (!this.EncryptionKey.IsDefaultArray()) 
                logger.LogInformation("{padding} Encryption Key: {value}", padding, this.EncryptionKey.ToDisplayString());
            if (this.SizeOfSizeOfEncryptedEncryptionPasswordKey > 0) 
                logger.LogInformation("{padding} Encryption Data 1: {size}, Second Part of Key: {secondPartOfKey}", padding, this.SizeOfSizeOfEncryptedEncryptionPasswordKey, DataUtils.ToString(this.SecondPartOfEncryptedEncryptionPasswordKey));
            if (this.SizeOfEncryptedEncryptionBabKey > 0) 
                logger.LogInformation("{padding} Encryption Data 2: {size}, Second Part of Key: {secondPartOfKey}", padding, this.SizeOfEncryptedEncryptionBabKey, DataUtils.ToString(this.SecondPartOfEncryptedEncryptionBabKey));
            
            // OS2 Info:
            if (this.BackupDateOs2 != 0)
                logger.LogInformation("{padding} Backup Date Os2: {value:X4}", padding, this.BackupDateOs2);
            if (this.BackupTimeOs2 != 0)
                logger.LogInformation("{padding} Backup Time Os2: {value:X4}", padding, this.BackupTimeOs2); 
            if (!this.IndexFileOs2.IsDefaultArray())
                logger.LogInformation("{padding} Index File Os2: {value}", padding, this.IndexFileOs2.ToDisplayString());

            // Misc Values.
            if (this.UnixFileSystemNameLength != 0)
                logger.LogInformation("{padding} Unix File System Name Length: {value}", padding, this.UnixFileSystemNameLength); 
            if (this.LastSession != 0)
                logger.LogInformation("{padding} Last Session: {value}", padding, this.LastSession); 
            if (this.ExtendedSessionHeader != 0)
                logger.LogInformation("{padding} Extended Session Header: {value}", padding, this.ExtendedSessionHeader);
        }

        /// <summary>
        /// Reads an ArcServe session header.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="header">The output storage for the header.</param>
        public static void ReadSessionHeader(DataReader reader, out ArcServeSessionHeader header) {
            ArcServeSessionHeaderSignature signature = reader.ReadEnum<uint, ArcServeSessionHeaderSignature>();
            ReadSessionHeader(reader, signature, out header);
        }

        /// <summary>
        /// Reads an ArcServe session header.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="signature">The signature of the session header.</param>
        /// <param name="header">The output storage for the header.</param>
        public static void ReadSessionHeader(DataReader reader, ArcServeSessionHeaderSignature signature, out ArcServeSessionHeader header) {
            header = new ArcServeSessionHeader();
            header.Signature = signature;
            header.RootDirectoryPath = reader.ReadFixedSizeString(128);
            header.UserName = reader.ReadFixedSizeString(48);
            header.Password = reader.ReadFixedSizeString(24);
            header.Description = reader.ReadFixedSizeString(80);
            header.Type = reader.ReadEnum<ushort, ArcServeSessionType>();
            header.Mode = reader.ReadEnum<byte, ArcServeSessionMode>();
            header.Flags = reader.ReadBitFlagEnum<uint, ArcServeSessionFlags>();
            header.CompressionType = reader.ReadEnum<byte, ArcServeCompressionType>();
            header.CompressionLevel = reader.ReadByte();
            header.UnixFileSystemNameLength = reader.ReadByte();
            header.SizeOfSizeOfEncryptedEncryptionPasswordKey = reader.ReadByte();
            header.SizeOfEncryptedEncryptionBabKey = reader.ReadByte();
            header.SecondPartOfEncryptedEncryptionPasswordKey = reader.ReadBytes(12);
            header.SecondPartOfEncryptedEncryptionBabKey = reader.ReadBytes(12);
            header.Version = reader.ReadUInt32();
            reader.SkipBytesRequireEmpty(8);
            header.TapeNumber = reader.ReadUInt16();
            header.StartTime = ArcServe.ParseTimeStamp(reader.ReadUInt32(ByteEndian.BigEndian), 1900);
            _ = reader.ReadByte();
            reader.SkipBytesRequire(1, 1);
            header.WorkStationType = reader.ReadEnum<byte, ArcServeWorkStationType>();
            header.WorkStationName = reader.ReadFixedSizeString(64);
            header.CompressionMethod = reader.ReadEnum<byte, ArcServeCompressionMethod>();
            header.BackupDateOs2 = reader.ReadUInt16();
            header.BackupTimeOs2 = reader.ReadUInt16();
            header.IndexFileOs2 = reader.ReadBytes(9);
            header.LastSession = reader.ReadByte();
            reader.SkipBytesRequireEmpty(4);
            header.ExtendedSessionHeader = reader.ReadUInt16();
            header.EncryptionKey = reader.ReadBytes(24);
            reader.SkipBytesRequireEmpty(62);
        }
    }
    
    /// <summary>
    /// Represents the session header signature.
    /// </summary>
    public enum ArcServeSessionHeaderSignature : uint
    {
        Default = 0xDDDDDDDDU,
        Signature386 = 0xDDDDD386U,
        Unknown = 0x5555AAAAU
    }

    public enum ArcServeSessionType : ushort
    {
        NetWare2X = 10,
        NetWare3X = 11, // Andy Borman's Tape
        NetWare4X = 12,
        NetWareImageSession = 13,
        WorkStationPc = 20,
        WorkStationMac = 23,
        WorkStationUnix = 24,
        ArcServeDatabase = 25,
        MicrosoftNetworkDrive = 27, // All Chicken Run Tapes
        ArcServeNtDatabase = 29,
        UnixImage = 38,
        WindowsNtFat16 = 40,
        WindowsNtNtfs = 42, // Frogger 2 EOP
        Windows95Fat16 = 43,
        WindowsNtImage = 46,
        Windows95Fat32 = 47,
        Windows2000Image = 49,
        WindowsNtFat32 = 60,
    }
    
    public enum ArcServeSessionMode : byte {
        AutoPilotFullBackup = 1,
        AutoPilotDifferentModificationDate = 2,
        AutoPilotDifferentArchBit = 3,
        AutoPilotIncreaseArchBit = 4,
        AutoPilotFullWeeklyBackup = 5,
        AutoPilotFullMonthlyBackup = 6,
        AutoPilotIncreaseModificationDate = 7,
        
        FullBackup = 10,
        FullClearArchive = 11,
        Incremental = 12,
        Differential = 13,
        FullLevel1 = 14,
        FullLevel2 = 15
        
        // Andy Borman = 0x01 (AutoPilotFullBackup)
        // Frogger = 0x0A (FullBackup)
        // Chicken Run = 0x0A (FullBackup)
    }

    [Flags]
    public enum ArcServeSessionFlags : uint
    {
        HasSystemObject = DataConstants.BitFlag0,
        VolumeLevel = DataConstants.BitFlag1,
        Filtered = DataConstants.BitFlag2,
        Pruned = DataConstants.BitFlag3,
        FullSms = DataConstants.BitFlag4,
        PartialSms = DataConstants.BitFlag5,
        BackupWithDirectoryServicesMode = DataConstants.BitFlag6,
        BackupWithAgent = DataConstants.BitFlag7,
        NetWareReserved0 = DataConstants.BitFlag8,
        NetWareReserved1 = DataConstants.BitFlag9,
        Compressed = DataConstants.BitFlag10,
        Encrypted = DataConstants.BitFlag11,
        HasCatalogueSession = DataConstants.BitFlag12,
        NoDetailInRecords = DataConstants.BitFlag13,
        HasCrcChecksum = DataConstants.BitFlag14,
        IsCheckpoint = DataConstants.BitFlag15,
        HasEisaConfigFile = DataConstants.BitFlag16,
        BackedUpToMbo = DataConstants.BitFlag17,
        IsMicrosoftClusterSharedDisk = DataConstants.BitFlag18,
        ContainsSisConfigFile = DataConstants.BitFlag20,
        ServerlessSessionType = DataConstants.BitFlag21,
        IsReplicated = DataConstants.BitFlag25,
        IsSessionPasswordEncrypted = DataConstants.BitFlag27,
        MergeDatabaseIncomplete = DataConstants.BitFlag28,
        HasVirtualPath = DataConstants.BitFlag29,
        CatalogUpdates = unchecked((uint)DataConstants.BitFlag31)
        
        // Andy Borman = 0x0000007 (Filtered | VolumeLevel | HasSystemObject)
        // Frogger 2 = 0x00007400 (HasCrcChecksum | NoDetailInRecords | HasCatalogueSession | Compressed)
        // Chicken Run = 0x00003002 (NoDetailInRecords | HasCatalogueSession | VolumeLevel)
    }

    /// <summary>
    /// Represents the available compression types.
    /// </summary>
    public enum ArcServeCompressionType : byte
    {
        Any,
        Pkware,
        Gnu
    }
    
    /// <summary>
    /// Represents the available compression types.
    /// </summary>
    public enum ArcServeCompressionMethod : byte
    {
        None,
        MaximumSpeed, // STAC LZS221 maximum speed
        MinimumSize, // STAC LZS221 minimum size
        Unknown
    }
    
    /// <summary>
    /// Represents the available workstation types.
    /// </summary>
    public enum ArcServeWorkStationType : byte
    {
        Default = 0, // PC or File server?
        Ipx = 1, // Network Address
        Mac = 2, // Machine
        Unix = 3, // Host Name
        MsNetwork = 4, // No Name
        Tcp = 5, // IP Address
        WindowsNt = 6 // Machine Name
    }
}