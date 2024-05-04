using OnStreamTapeLibrary.Position;
using System;
using System.Collections.Concurrent;

namespace OnStreamTapeLibrary
{
    
    /// <summary>
    /// Represents the type of OnStream cartridge.
    /// </summary>
    public enum OnStreamCartridgeType
    {
        Raw, // Represents a tape dump which has no OnStream data chunk. (Eg: A pure data stream)
        
        Adr30, // Generation 1, 15GB Physical Capacity. (30GB Compressed, assuming 2:1 compression ratio)
        Adr50, // Generation 1, 25GB Physical Capacity. (50GB Compressed, assuming 2:1 compression ratio)
        
        // Currently unsupported.
        Adr60, // Generation 2, 30GB Physical Capacity. (60GB Compressed, assuming 2:1 compression ratio)
        Adr120, // Generation 2, 60GB Physical Capacity. (120GB Compressed, assuming 2:1 compression ratio)
    }

    /// <summary>
    /// Static extension methods for cartridge types.
    /// </summary>
    public static class OnStreamCartridgeTypeExtensions
    {
        private static readonly ConcurrentDictionary<OnStreamCartridgeType, OnStreamPhysicalPosition> TemporaryPositions = new ConcurrentDictionary<OnStreamCartridgeType, OnStreamPhysicalPosition>();
        
        /// <summary>
        /// Gets the number of physical tracks on a certain kind of tape.
        /// NOTE: 8 physical tracks are read/written at once.
        /// This means that dividing this number by eight will give the total number of readable data streams.
        /// Because this is handled by the drive for us, we call a group of 8 physical tracks a single track, or a logical track.
        /// The term "physical track" is used to distinguish between the two different types.
        /// </summary>
        /// <param name="type">The cartridge type.</param>
        /// <returns>The number of physical tracks.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid cartridge type is specified.</exception>
        public static ushort GetPhysicalTrackCount(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Adr30 => 192,
                OnStreamCartridgeType.Adr50 => 192,
                OnStreamCartridgeType.Adr60 => 384,
                OnStreamCartridgeType.Adr120 => 384,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        /// <summary>
        /// Gets the number of logical tracks on a certain kind of tape.
        /// </summary>
        /// <param name="type">The cartridge type.</param>
        /// <returns>The number of logical tracks.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if an invalid cartridge type is specified.</exception>
        public static ushort GetLogicalTrackCount(this OnStreamCartridgeType type) {
            return (ushort)(GetPhysicalTrackCount(type) / 8);
        }

        /// <summary>
        /// A frame (aka block) is a 32.5kb chunk of data with a 32kb user-data section and a 512b aux section.
        /// Gets the number of frames on a single logical track. Not all of the frames may be accessible.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>trackFrameCount</returns>
        public static uint GetTrackFrameCount(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Adr30 => OnStreamPhysicalPositionAdr30.FramesPerTrack,
                OnStreamCartridgeType.Adr50 => OnStreamPhysicalPositionAdr50.FramesPerTrack,
                _ => throw new OnStreamCartridgeTypeException(type, "Don't know the per-track frame count")
            };
        }
        
        /// <summary>
        /// Check if this tape type has a parking zone.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>logicalBlockCount</returns>
        public static bool HasParkingZone(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Raw => false,
                OnStreamCartridgeType.Adr30 => false,
                OnStreamCartridgeType.Adr50 => true,
                _ => throw new OnStreamCartridgeTypeException(type, "Don't know if a parking zone exists")
            };
        }
        
        /// <summary>
        /// Gets the position which the parking zone ends. (First
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>logicalBlockCount</returns>
        public static ushort GetParkingZoneEnd(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Adr30 => throw new OnStreamCartridgeTypeException(type, "There is no parking zone"),
                OnStreamCartridgeType.Adr50 => OnStreamPhysicalPositionAdr50.ParkingZoneEnd,
                _ => throw new OnStreamCartridgeTypeException(type, "Don't know if a parking zone exists")
            };
        }

        /// <summary>
        /// Gets the total number of logical blocks which can be written to on a tape.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>logicalBlockCount</returns>
        public static uint GetLogicalBlockCount(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Adr30 => OnStreamPhysicalPositionAdr30.LogicalBlockCount,
                OnStreamCartridgeType.Adr50 => OnStreamPhysicalPositionAdr50.LogicalBlockCount,
                _ => throw new OnStreamCartridgeTypeException(type, "Don't know the logical block count")
            };
        }
        
        /// <summary>
        /// Gets the maximum logical block which can be written to on a tape.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>The maximum logical block number which can be read.</returns>
        public static uint GetMaxLogicalBlock(this OnStreamCartridgeType type) {
            return GetLogicalBlockCount(type) - 1;
        }

        /// <summary>
        /// Creates an <see cref="OnStreamPhysicalPosition"/> instance which works for the particular tape cartridge type.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <returns>newPositionInstance</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static OnStreamPhysicalPosition CreatePosition(this OnStreamCartridgeType type) {
            return type switch {
                OnStreamCartridgeType.Adr30 => new OnStreamPhysicalPositionAdr30(),
                OnStreamCartridgeType.Adr50 => new OnStreamPhysicalPositionAdr50(),
                _ => throw new OnStreamCartridgeTypeException(type, "The behavior class has not been implemented")
            };
        }
        
        private static OnStreamPhysicalPosition GetTemporaryPosition(this OnStreamCartridgeType type) {
            return TemporaryPositions.GetOrAdd(type, key => key.CreatePosition());
        }
        
        /// <summary>
        /// Creates an <see cref="OnStreamPhysicalPosition"/> instance from loading a physical block number.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="physicalBlock">The physical block number.</param>
        /// <returns>newPositionInstance</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static OnStreamPhysicalPosition FromPhysicalBlock(this OnStreamCartridgeType type, uint physicalBlock) {
            OnStreamPhysicalPosition physicalPosition = CreatePosition(type);
            physicalPosition.FromPhysicalBlock(physicalBlock);
            return physicalPosition;
        }
        
        /// <summary>
        /// Creates an <see cref="OnStreamPhysicalPosition"/> instance which represents the provided logical block.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="logicalBlock">The logical block number to load from.</param>
        /// <returns>newPositionInstance</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static OnStreamPhysicalPosition FromLogicalBlock(this OnStreamCartridgeType type, uint logicalBlock) {
            OnStreamPhysicalPosition physicalPosition = CreatePosition(type);
            physicalPosition.FromLogicalBlock(logicalBlock);
            return physicalPosition;
        }
        
        /// <summary>
        /// Converts a logical block number into a physical block number.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="logicalBlock">The logical block number to convert.</param>
        /// <returns>physicalBlock</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static uint ConvertLogicalBlockToPhysicalBlock(this OnStreamCartridgeType type, uint logicalBlock) {
            OnStreamPhysicalPosition tempPos = GetTemporaryPosition(type);
            lock (tempPos)
            {
                tempPos.FromLogicalBlock(logicalBlock);
                return tempPos.ToPhysicalBlock();
            }
        }
        
        /// <summary>
        /// Converts a physical block number into a logical block number.
        /// An exception is thrown if the physical block cannot be represented as a logical block number.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="physicalBlock">The physical block number to convert.</param>
        /// <returns>logicalBlock</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static uint ConvertPhysicalBlockToLogicalBlock(this OnStreamCartridgeType type, uint physicalBlock) {
            OnStreamPhysicalPosition tempPos = GetTemporaryPosition(type);
            lock (tempPos)
            {
                tempPos.FromPhysicalBlock(physicalBlock);
                return tempPos.ToLogicalBlock();
            }
        }
        
        /// <summary>
        /// Converts a logical block number into a physical block string representation.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="logicalBlock">The logical block number to convert.</param>
        /// <returns>physicalBlockString</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static string ConvertLogicalBlockToPhysicalBlockString(this OnStreamCartridgeType type, uint logicalBlock) {
            OnStreamPhysicalPosition tempPos = GetTemporaryPosition(type);
            lock (tempPos)
            {
                tempPos.FromLogicalBlock(logicalBlock);
                return tempPos.ToPhysicalBlockString();
            }
        }
        
        /// <summary>
        /// Converts a physical block number into a logical block string representation.
        /// This can show positions which aren't actually representable as a logical block.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="physicalBlock">The physical block number to convert.</param>
        /// <returns>logicalBlockString</returns>
        /// <exception cref="OnStreamCartridgeTypeException">Thrown if the cartridge type isn't supported.</exception>
        public static string ConvertPhysicalBlockToLogicalBlockString(this OnStreamCartridgeType type, uint physicalBlock) {
            OnStreamPhysicalPosition tempPos = GetTemporaryPosition(type);
            lock (tempPos)
            {
                tempPos.FromPhysicalBlock(physicalBlock);
                return tempPos.ToLogicalBlockString();
            }
        }
    }
}