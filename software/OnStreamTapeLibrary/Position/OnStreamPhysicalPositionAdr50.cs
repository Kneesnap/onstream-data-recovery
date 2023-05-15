using System;
using System.Data;

namespace OnStreamTapeLibrary.Position
{
    /// <summary>
    /// Represents the physical position of an OnStream ADR50 tape.
    /// </summary>
    public class OnStreamPhysicalPositionAdr50 : OnStreamPhysicalPosition
    {
        private byte _y;
        private ushort _x;

        /// <summary>
        /// A field representing the logical track (y coordinate) of the data block.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the provided track is not valid.</exception>
        public byte Track {
            get => this._y;
            set {
                if (value >= TrackCount)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Track must be less than {TrackCount}, but was assigned {value}.");

                this._y = value;
            }
        }
        
        /// <summary>
        /// Identify which part of the tape the position is in.
        /// </summary>
        public OnStreamTapeAddressableLocation Location {
            get {
                if (this.X >= ParkingZoneEnd) {
                    return OnStreamTapeAddressableLocation.FrontHalf;
                } else if (this.X < ParkingZoneStart) {
                    return OnStreamTapeAddressableLocation.BackHalf;
                } else {
                    return OnStreamTapeAddressableLocation.ParkingZone;
                }
            }
        }
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.X"/>
        public override ushort X {
            get => this._x;
            set {
                if (value >= FramesPerTrack)
                    throw new ArgumentOutOfRangeException(nameof(value), $"X must be less than {FramesPerTrack}, but was assigned {value}.");

                this._x = value;
            }
        }
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.Y"/>
        public override ushort Y {
            get => this._y;
            set {
                if (value >= TrackCount)
                    throw new ArgumentOutOfRangeException(nameof(value), $"Track must be less than {TrackCount}, but was assigned {value}.");

                this._y = (byte)value;
            }
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.IsRepresentableAsLogicalBlock"/>
        public override bool IsRepresentableAsLogicalBlock => !this.IsParkingZone;
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.IsBeforeParkingZone"/>
        public override bool IsBeforeParkingZone => (this.Location == OnStreamTapeAddressableLocation.BackHalf);
        /// <inheritdoc cref="OnStreamPhysicalPosition.IsParkingZone"/>
        public override bool IsParkingZone => (this.Location == OnStreamTapeAddressableLocation.ParkingZone);
        /// <inheritdoc cref="OnStreamPhysicalPosition.IsAfterParkingZone"/>
        public override bool IsAfterParkingZone => (this.Location == OnStreamTapeAddressableLocation.FrontHalf);

        public const int ParkingZoneFrameCount = 99;
        public const int FramesPerTrack = 31959;
        public const int LogicalBlockCount = (FramesPerTrack - ParkingZoneFrameCount) * TrackCount;
        public const int LowerHalfFastLaneStart = (LogicalBlockCount / 2) - HalfTapeSegmentCount; // Inclusive.
        public const int UpperHalfFastLaneStart = LogicalBlockCount - HalfTapeSegmentCount; // Inclusive.

        public const int HalfTapeSegmentCount = ParkingZoneStart;
        public const int MaxLocalPartition = HalfTapeSegmentCount / BlocksPerTrackSegment;
        public const int BlocksPerEdgeTrackSegment = HalfTapeSegmentCount % BlocksPerTrackSegment;

        public const int TrackCount = 24;
        public const int TrackCountWithoutFastLane = TrackCount - 1;
        public const int BlocksPerTrackSegment = 1500;
        public const int BlocksPerPartitionMinusFastLane = (BlocksPerTrackSegment * (TrackCount - 1));

        public const int ParkingZoneEnd = (FramesPerTrack + ParkingZoneFrameCount) / 2; // Exclusive aka <
        public const int ParkingZoneStart = ((FramesPerTrack - ParkingZoneFrameCount) / 2); // Inclusive aka >=

        public OnStreamPhysicalPositionAdr50() : base(OnStreamCartridgeType.Adr50) {
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.ToLogicalBlockString"/>
        public override string ToLogicalBlockString() {
            if (this.Location == OnStreamTapeAddressableLocation.ParkingZone) {
                return this.X switch {
                    ParkingZoneStart => "<start of parking zone>",
                    ParkingZoneEnd - 1 => this.Track == 0 ? "<end/start of tape>" : "<end of parking zone>",
                    _ => $"<parking zone: {this.X - ParkingZoneStart}>"
                };
            }

            return this.ToLogicalBlock().ToString();
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.ToLogicalBlock"/>
        public override uint ToLogicalBlock() {
            return this.Location switch {
                OnStreamTapeAddressableLocation.BackHalf => this.ToLogicalBlockSecondHalf(),
                OnStreamTapeAddressableLocation.ParkingZone => throw new Exception("Positions in the parking zone cannot be represented as a logical block."),
                OnStreamTapeAddressableLocation.FrontHalf => this.ToLogicalBlockFirstHalf(),
                _ => throw new ArgumentOutOfRangeException(nameof(this.Location))
            };
        }

        private uint ToLogicalBlockFirstHalf() {
            if (this.Track == TrackCount - 1) // Fast Lane
                return (uint)((LogicalBlockCount / 2) - 1 - (this.X - ParkingZoneEnd));

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

            uint result = (LogicalBlockCount / 2);

            ushort pos = (ushort)(ParkingZoneStart - this.X - 1);
            ushort localFrame = (ushort)(pos % BlocksPerTrackSegment);
            ushort partition = (ushort)(pos / BlocksPerTrackSegment);
            uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

            result += (uint)(partition * BlocksPerPartitionMinusFastLane);

            if ((partition % 2) > 0) { // Low track number to high track number.
                result += (uint)((this.Track - 1) * blocksPerTrack); // Subtracting 1 removes the fast lane at 0.
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

        /// <inheritdoc cref="OnStreamPhysicalPosition.ToPhysicalBlock"/>
        public override uint ToPhysicalBlock() {
            return (uint)((this.Track << 24) + this.X);
        }
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.FromLogicalBlock"/>
        public override void FromLogicalBlock(uint logicalBlock) {
            if (logicalBlock >= LogicalBlockCount) {
                throw new DataException($"Logical Block {logicalBlock} is larger than the physical capacity of the tape! Block Range: [0, {LogicalBlockCount})");
            } else if (logicalBlock >= UpperHalfFastLaneStart) { // Fast lane.
                this.Track = 0;
                this.X = (ushort)(logicalBlock - UpperHalfFastLaneStart);
            } else if (logicalBlock >= (LogicalBlockCount / 2)) { // Past halfway point.
                logicalBlock -= (LogicalBlockCount / 2);
                ushort partition = (ushort)(logicalBlock / BlocksPerPartitionMinusFastLane);
                logicalBlock %= BlocksPerPartitionMinusFastLane;
                uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

                if (partition % 2 > 0) { // Low track number to highest track number.
                    this.Track = (byte)((logicalBlock / blocksPerTrack) + 1);
                } else { // Highest track number to lowest track number.
                    this.Track = (byte)(TrackCountWithoutFastLane - (logicalBlock / blocksPerTrack));
                }

                ushort localFrame = (ushort)(logicalBlock % blocksPerTrack);
                this.X = (ushort)(ParkingZoneStart - (partition * BlocksPerTrackSegment));
                if ((this.Track % 2) > 0) { // Track is read in opposite direction, eg: the physical number is increasing as the actual block number decreases.
                    this.X -= (ushort)(localFrame + 1);
                } else { // Reading in the normal direction (From front reel to back reel)
                    this.X -= (ushort)(blocksPerTrack - localFrame);
                }
            } else if (logicalBlock >= LowerHalfFastLaneStart) { // Fast lane.
                this.Track = TrackCountWithoutFastLane;
                this.X = (ushort)(FramesPerTrack - (logicalBlock - LowerHalfFastLaneStart) - 1);
            } else { // Before halfway point.
                ushort partition = (ushort)(logicalBlock / BlocksPerPartitionMinusFastLane);
                logicalBlock %= BlocksPerPartitionMinusFastLane;
                uint blocksPerTrack = (uint)(partition == MaxLocalPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

                if (partition % 2 > 0) { // Highest track to lowest track.
                    this.Track = (byte)(TrackCountWithoutFastLane - (logicalBlock / blocksPerTrack) - 1);
                } else { // Lowest track to highest track.
                    this.Track = (byte)(logicalBlock / blocksPerTrack);
                }

                this.X = (ushort)(ParkingZoneEnd + (BlocksPerTrackSegment * partition));
                if (this.Track % 2 > 0) { // Backwards direction.
                    this.X += (ushort)(blocksPerTrack - (logicalBlock % blocksPerTrack) - 1);
                } else { // Forwards direction.
                    this.X += (ushort)(logicalBlock % blocksPerTrack);
                }
            }
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.FromPhysicalBlock"/>
        public override void FromPhysicalBlock(uint physicalBlock) {
            this.Track = (byte)((physicalBlock >> 24) & 0xFF);
            byte zero = (byte)((physicalBlock >> 16) & 0xFF);
            if (zero != 0)
                throw new DataException($"The third byte in '{physicalBlock:X8}' was expected to be zero, but was not!");

            this.X = (ushort)(physicalBlock & 0xFFFF);
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.TryIncreasePhysicalBlock"/>
        public override bool TryIncreasePhysicalBlock(bool skipParkingZone = false) {
            return skipParkingZone
                ? this.TryIncrementPhysicalBlockSkipParkingZone()
                : this.TryIncrementPhysicalBlockIncludeParkingZone();
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.Clone"/>
        public override OnStreamPhysicalPositionAdr50 Clone() {
            OnStreamPhysicalPositionAdr50 newObj = new OnStreamPhysicalPositionAdr50();
            newObj._x = this._x;
            newObj._y = this._y;
            return newObj;
        }
        
        /// <summary>
        /// Increments an OnStream physical position to the next position which ArcServe would read.
        /// This may only apply to ArcServe because most software incremented the logical block number like the documentation said to.
        /// ArcServe instead reads in a straight line until it hits the end of the tape.
        /// </summary>
        /// <returns>If the increment was successful.</returns>
        public bool TryIncrementPhysicalBlockSkipParkingZone() {
            if ((this.Track % 2) == 0) {
                if (this.X == ParkingZoneStart - 1) { // Reached parking zone.
                    if (this.Track == 0) { // End of tape.
                        return false;
                    } else { // Skip parking zone.
                        this.X = ParkingZoneEnd;
                    }
                } else if (this.X >= FramesPerTrack - 1) { // End of track.
                    this.Track++;
                } else {
                    this.X++;
                }
            } else if (this.X == ParkingZoneEnd) { // Skip parking zone.
                this.X = ParkingZoneStart - 1;
            } else if (this.X == 0) { // End of track.
                if (this.Track == TrackCount - 1) { // Last track! This is a guess that the behavior works this way, but it's likely.
                    this.Track = 0;
                } else { // Reached beginning of track, let's move tracks.
                    this.Track++;
                }
            } else {
                this.X--;
            }

            return true;
        }
        
        /// <summary>
        /// Increments an OnStream physical position to the next position which ArcServe would read.
        /// This may only apply to ArcServe because most software incremented the logical block number like the documentation said to.
        /// ArcServe instead reads in a straight line until it hits the end of the tape.
        /// </summary>
        /// <returns>If the increment was successful.</returns>
        public bool TryIncrementPhysicalBlockIncludeParkingZone() {
            if ((this.Track % 2) == 0) {
                if (this.X == ParkingZoneEnd - 1 && this.Track == 0) { // End of parking zone.
                    return false;
                } else if (this.X >= FramesPerTrack - 1) { // End of track.
                    this.Track++;
                } else {
                    this.X++;
                }
            } else if (this.X == 0) { // End of track.
                if (this.Track == TrackCount - 1) { // Last track! This is a guess that the behavior works this way, but it's likely.
                    this.Track = 0;
                } else { // Reached beginning of track, let's move tracks.
                    this.Track++;
                }
            } else {
                this.X--;
            }

            return true;
        }
    }

    public enum OnStreamTapeAddressableLocation
    {
        BackHalf, // >= Block 382320
        ParkingZone, // The center of the tape portion before block 0 where the drive positions the tape for an eject.
        FrontHalf // < Block 382320
    }
}