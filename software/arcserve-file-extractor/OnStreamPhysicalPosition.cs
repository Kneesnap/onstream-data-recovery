using System;
using System.Data;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Represents a physical position of a block of tape in an ADR50 tape.
    /// </summary>
    public struct OnStreamPhysicalPosition
    {
        private byte _y;
        private ushort _x;

        public byte Track {
            get => this._y;
            set {
                if (value >= TrackCount)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Track must be less than {TrackCount}, but was assigned {value}.");

                this._y = value;
            }
        }

        public ushort X {
            get => this._x;
            set {
                if (value >= FramesPerTrack)
                    throw new ArgumentOutOfRangeException(nameof(value), $"X must be less than {FramesPerTrack}, but was assigned {value}.");

                this._x = value;
            }
        }

        /// <summary>
        /// Identify which part of the tape the position is in.
        /// </summary>
        public OnStreamTapeAddressableLocation Location {
            get
            {
                if (this.X >= ParkingZoneEnd) {
                    return OnStreamTapeAddressableLocation.FrontHalf;
                } else if (this.X < ParkingZoneStart) {
                    return OnStreamTapeAddressableLocation.BackHalf;
                } else {
                    return OnStreamTapeAddressableLocation.ParkingZone;
                }
            }
        }
        
        public const int ParkingZoneFrameCount = 99;
        public const int FramesPerTrack = 31959;
        public const int MaxLogicalBlock = (FramesPerTrack - ParkingZoneFrameCount) * TrackCount;
        public const int LowerHalfFastLaneStart = (MaxLogicalBlock / 2) - HalfTapeSegmentCount; // Inclusive.
        public const int UpperHalfFastLaneStart = MaxLogicalBlock - HalfTapeSegmentCount; // Inclusive.
        
        public const int HalfTapeSegmentCount = ParkingZoneStart;
        public const int MaxLocalPartition = HalfTapeSegmentCount / BlocksPerTrackSegment;
        public const int BlocksPerEdgeTrackSegment = HalfTapeSegmentCount % BlocksPerTrackSegment;

        public const int TrackCount = 24;
        public const int TrackCountWithoutFastLane = TrackCount - 1;
        public const int BlocksPerTrackSegment = 1500;
        public const int BlocksPerPartitionMinusFastLane = (BlocksPerTrackSegment * (TrackCount - 1));
        
        public const int ParkingZoneEnd = (FramesPerTrack + ParkingZoneFrameCount) / 2; // Exclusive aka <
        public const int ParkingZoneStart = ((FramesPerTrack - ParkingZoneFrameCount) / 2); // Inclusive aka >=

        /// <summary>
        /// Gets this position as a logical block string.
        /// Useful in situations where this might be in the parking zone and therefore does not have a logical block.
        /// </summary>
        /// <returns>logicalBlockString</returns>
        public string ToLogicalBlockString() {
            if (this.Location == OnStreamTapeAddressableLocation.ParkingZone) {
                return this.X switch {
                    ParkingZoneStart => "<start of parking zone>",
                    ParkingZoneEnd - 1 => this.Track == 0 ? "<end/start of tape>" : "<end of parking zone>",
                    _ => $"<parking zone: {this.X - ParkingZoneStart}>"
                };
            }
            
            return this.ToLogicalBlock().ToString();
        }

        /// <summary>
        /// Converts the physical position into a logical block number for an ADR 50GB tape.
        /// </summary>
        /// <returns>logicalBlock</returns>
        public uint ToLogicalBlock() {
            return this.Location switch {
                OnStreamTapeAddressableLocation.BackHalf => this.ToLogicalBlockSecondHalf(),
                OnStreamTapeAddressableLocation.ParkingZone => throw new Exception("Positions in the parking zone cannot be represented as a logical block."),
                OnStreamTapeAddressableLocation.FrontHalf => this.ToLogicalBlockFirstHalf(),
                _ => throw new ArgumentOutOfRangeException(nameof(this.Location))
            };
        }

        private uint ToLogicalBlockFirstHalf() {
            if (this.Track == TrackCount - 1) // Fast Lane
                return (uint)((MaxLogicalBlock / 2) - 1 - (this.X - ParkingZoneEnd));

            ushort x = (ushort)(this.X - ParkingZoneEnd);
            ushort localFrame = (ushort)(x % BlocksPerTrackSegment);
            ushort partition = (ushort)(x / BlocksPerTrackSegment);

            uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);
            
            uint result = (uint)(partition * BlocksPerPartitionMinusFastLane);
            if ((partition % 2) > 0) { // High track number to low track number.
                result += (uint)((TrackCountWithoutFastLane - this.Track - 1) * blocksPerTrack);
            } else { // Low track number to high track number.
                result += (this.Track * blocksPerTrack);
            }
            
            if ((this.Track % 2) > 0) { // Track is read in opposite direction, eg: the physical number is decreasing as the actual block number increases.
                result += (blocksPerTrack - localFrame - 1);
            } else { // Reading in the normal direction (Tape moves from front reel to back reel)
                result += localFrame;
            }

            return result;
        }
        
        private uint ToLogicalBlockSecondHalf() {
            if (this.Track == 0) // Fast Lane
                return (uint)(UpperHalfFastLaneStart + this.X);
            
            uint result = (MaxLogicalBlock / 2);

            ushort pos = (ushort)(ParkingZoneStart - this.X - 1);
            ushort localFrame = (ushort)(pos % BlocksPerTrackSegment);
            ushort partition = (ushort)(pos / BlocksPerTrackSegment);
            uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

            result += (uint)(partition * BlocksPerPartitionMinusFastLane);
            
            if ((partition % 2) > 0) { // Low track number to high track number.
                result += (uint) ((this.Track - 1) * blocksPerTrack); // Subtracting 1 removes the fast lane at 0.
            } else { // High track number to low track number.
                result += (uint)((TrackCountWithoutFastLane - this.Track) * blocksPerTrack);
            }

            if ((this.Track % 2) > 0) { // Track is read in opposite direction, eg: the physical number is increasing as the actual block number decreases.
                result += localFrame;
            } else { // Reading in the normal direction (From front reel to back reel)
                result += (blocksPerTrack - localFrame - 1);
            }

            return result;
        }

        /// <summary>
        /// Converts the physical position into a physical block number for an ADR 50GB tape.
        /// </summary>
        /// <returns>physicalPosNumber</returns>
        public uint ToPhysicalBlock() {
            return (uint)((this.Track << 24) + this.X);
        }

        /// <summary>
        /// Loads a physical position struct from a uint32 physical position from a 50GB ADR tape.
        /// </summary>
        /// <param name="physicalPos">The integer to parse.</param>
        /// <param name="pos">The output storage for the physical position</param>
        public static void FromPhysicalBlock(uint physicalPos, out OnStreamPhysicalPosition pos) {
            pos = default;
            pos.Track = (byte)((physicalPos >> 24) & 0xFF);
            if (pos.Track >= TrackCount)
                throw new DataException($"Invalid track {pos.Track} in {physicalPos:X8}!");

            byte zero = (byte)((physicalPos >> 16) & 0xFF);
            if (zero != 0)
                throw new DataException($"The third byte in {physicalPos:X8} was expected to be zero, but was not!");

            pos.X = (ushort)(physicalPos & 0xFFFF);
        }

        /// <summary>
        /// Converts a logical block position from an ADR 50Gb tape into a physical tape position.
        /// </summary>
        /// <param name="logicalBlock">The logical block to convert.</param>
        /// <param name="pos">Output storage.</param>
        public static void FromLogicalBlock(uint logicalBlock, out OnStreamPhysicalPosition pos) {
            pos = default;

            if (logicalBlock >= MaxLogicalBlock) {
                throw new DataException($"Logical Block {logicalBlock} is larger than the physical capacity of the tape! Block Range: [0, {MaxLogicalBlock})");
            } else if (logicalBlock >= UpperHalfFastLaneStart) { // Fast lane.
                pos.Track = 0;
                pos.X = (ushort) (logicalBlock - UpperHalfFastLaneStart);
            } else if (logicalBlock >= (MaxLogicalBlock / 2)) { // Past halfway point.
                logicalBlock -= (MaxLogicalBlock / 2);
                ushort partition = (ushort)(logicalBlock / BlocksPerPartitionMinusFastLane);
                logicalBlock %= BlocksPerPartitionMinusFastLane;
                uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

                if (partition % 2 > 0) { // Low track number to highest track number.
                    pos.Track = (byte)((logicalBlock / blocksPerTrack) + 1);
                } else { // Highest track number to lowest track number.
                    pos.Track = (byte)(TrackCountWithoutFastLane - (logicalBlock / blocksPerTrack));
                }

                ushort localFrame = (ushort)(logicalBlock % blocksPerTrack);
                pos.X = (ushort)(ParkingZoneStart - (partition * BlocksPerTrackSegment));
                if ((pos.Track % 2) > 0) { // Track is read in opposite direction, eg: the physical number is increasing as the actual block number decreases.
                    pos.X -= (ushort)(localFrame + 1);
                } else { // Reading in the normal direction (From front reel to back reel)
                    pos.X -= (ushort)(blocksPerTrack - localFrame);
                }
            } else if (logicalBlock >= LowerHalfFastLaneStart) { // Fast lane.
                pos.Track = TrackCountWithoutFastLane;
                pos.X = (ushort)(FramesPerTrack - (logicalBlock - LowerHalfFastLaneStart) - 1);
            } else { // Before halfway point.
                ushort partition = (ushort)(logicalBlock / BlocksPerPartitionMinusFastLane);
                logicalBlock %= BlocksPerPartitionMinusFastLane;
                uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

                if (partition % 2 > 0) { // Highest track to lowest track.
                    pos.Track = (byte)(TrackCountWithoutFastLane - (logicalBlock / blocksPerTrack) - 1);
                } else { // Lowest track to highest track.
                    pos.Track = (byte)(logicalBlock / blocksPerTrack);
                }
                
                pos.X = (ushort)(ParkingZoneEnd + (BlocksPerTrackSegment * partition));
                if (pos.Track % 2 > 0) { // Backwards direction.
                    pos.X += (ushort)(blocksPerTrack - (logicalBlock % blocksPerTrack) - 1);
                } else { // Forwards direction.
                    pos.X += (ushort)(logicalBlock % blocksPerTrack);
                }
            }
        }

        /// <summary>
        /// Converts a logical block position from an ADR 50Gb tape into the corresponding physical block.
        /// </summary>
        /// <param name="logicalBlock">The logical block to convert.</param>
        /// <returns>physicalBlock</returns>
        public static uint ConvertLogicalBlockToPhysical(uint logicalBlock) {
            FromLogicalBlock(logicalBlock, out OnStreamPhysicalPosition pos);
            return pos.ToPhysicalBlock();
        }
        
        /// <summary>
        /// Converts a physical block position from an ADR 50Gb tape into the corresponding logical block.
        /// </summary>
        /// <param name="physicalBlock">The physical block to convert.</param>
        /// <returns>logicalBlock</returns>
        public static uint ConvertPhysicalBlockToLogical(uint physicalBlock) {
            FromPhysicalBlock(physicalBlock, out OnStreamPhysicalPosition pos);
            return pos.ToLogicalBlock();
        }
        
        /// <summary>
        /// Converts a physical block position from an ADR 50Gb tape into the corresponding logical block.
        /// </summary>
        /// <param name="physicalBlock">The physical block to convert.</param>
        /// <returns>logicalBlock</returns>
        public static string ConvertPhysicalBlockToLogicalString(uint physicalBlock) {
            FromPhysicalBlock(physicalBlock, out OnStreamPhysicalPosition pos);
            return pos.ToLogicalBlockString();
        }
    }
    
    public enum OnStreamTapeAddressableLocation
    {
        BackHalf, // >= Block 382320
        ParkingZone, // The center of the tape portion before block 0 where the drive positions the tape for an eject.
        FrontHalf // < Block 382320
    }
}