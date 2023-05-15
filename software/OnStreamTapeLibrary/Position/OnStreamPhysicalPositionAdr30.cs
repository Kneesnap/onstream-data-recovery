using System;
using System.Data;

namespace OnStreamTapeLibrary.Position
{
    /// <summary>
    /// Implements the physical position for an OnStream ADR-30 tape.
    /// </summary>
    public class OnStreamPhysicalPositionAdr30 : OnStreamPhysicalPosition
    {
        private byte _y;
        private ushort _x;
        
        public const int FramesPerTrack = 19239;
        public const int LogicalBlockCount = FramesPerTrack * TrackCount;
        
        public const int TrackCount = 24;
        public const int TrackCountWithoutFastLane = TrackCount - 1;
        public const int BlocksPerTrackSegment = 1500;
        public const int BlocksPerPartitionMinusFastLane = (BlocksPerTrackSegment * (TrackCount - 1));
        public const int MaxPartition = FramesPerTrack / BlocksPerTrackSegment;
        public const int BlocksPerEdgeTrackSegment = FramesPerTrack % BlocksPerTrackSegment;
        public const int FastLaneStart = (LogicalBlockCount - FramesPerTrack);
        
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
        public override bool IsRepresentableAsLogicalBlock => true; // All data is representable as a logical block.
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.IsBeforeParkingZone"/>
        public override bool IsBeforeParkingZone => throw new OnStreamCartridgeTypeException(this.Type, "There is no parking zone");

        /// <inheritdoc cref="OnStreamPhysicalPosition.IsParkingZone"/>
        public override bool IsParkingZone => false; // These tapes have no parking zone.
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.IsAfterParkingZone"/>
        public override bool IsAfterParkingZone => throw new OnStreamCartridgeTypeException(this.Type, "There is no parking zone");

        public OnStreamPhysicalPositionAdr30() : base(OnStreamCartridgeType.Adr30)
        {
        }
        
        /// <inheritdoc cref="OnStreamPhysicalPosition.ToLogicalBlockString"/>
        public override string ToLogicalBlockString() {
            return this.ToLogicalBlock().ToString();
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.ToLogicalBlock"/>
        public override uint ToLogicalBlock() {
            if (this.Y == TrackCount - 1) // Fast Lane
                return (uint)(LogicalBlockCount - this.X - 1);

            ushort localFrame = (ushort)(this.X % BlocksPerTrackSegment);
            ushort partition = (ushort)(this.X / BlocksPerTrackSegment);

            uint blocksPerTrack = (uint)(partition == MaxPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

            uint result = (uint)(partition * BlocksPerPartitionMinusFastLane);
            if ((partition % 2) > 0) { // High track number to low track number.
                result += (uint)((TrackCountWithoutFastLane - this.Y - 1) * blocksPerTrack);
            } else { // Low track number to high track number.
                result += (this.Y * blocksPerTrack);
            }

            if ((this.Y % 2) > 0) { // Track is read in opposite direction, eg: the physical number is decreasing as the actual block number increases.
                result += (blocksPerTrack - localFrame - 1);
            } else { // Reading in the normal direction (Tape moves from front reel to back reel)
                result += localFrame;
            }

            return result;
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.ToPhysicalBlock"/>
        public override uint ToPhysicalBlock() {
            return (uint)((this.Y << 24) + this.X);
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.FromLogicalBlock"/>
        public override void FromLogicalBlock(uint logicalBlock) {
            if (logicalBlock >= LogicalBlockCount) {
                throw new DataException($"Logical Block {logicalBlock} is larger than the physical capacity of the tape! Block Range: [0, {LogicalBlockCount})");
            } else if (logicalBlock >= FastLaneStart) { // Fast lane.
                this.Y = TrackCount - 1;
                this.X = (ushort)(LogicalBlockCount - logicalBlock - 1);
            } else { // Before halfway point.
                ushort partition = (ushort)(logicalBlock / BlocksPerPartitionMinusFastLane);
                logicalBlock %= BlocksPerPartitionMinusFastLane;
                uint blocksPerTrack = (uint)(partition == MaxPartition ? BlocksPerEdgeTrackSegment : BlocksPerTrackSegment);

                if (partition % 2 > 0) { // Highest track to lowest track.
                    this.Y = (byte)(TrackCountWithoutFastLane - (logicalBlock / blocksPerTrack) - 1);
                } else { // Lowest track to highest track.
                    this.Y = (byte)(logicalBlock / blocksPerTrack);
                }

                this.X = (ushort)(BlocksPerTrackSegment * partition);
                if (this.Y % 2 > 0) { // Backwards direction.
                    this.X += (ushort)(blocksPerTrack - (logicalBlock % blocksPerTrack) - 1);
                } else { // Forwards direction.
                    this.X += (ushort)(logicalBlock % blocksPerTrack);
                }
            }
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.FromPhysicalBlock"/>
        public override void FromPhysicalBlock(uint physicalBlock) {
            this.Y = (byte)((physicalBlock >> 24) & 0xFF);
            byte zero = (byte)((physicalBlock >> 16) & 0xFF);
            if (zero != 0)
                throw new DataException($"The third byte in '{physicalBlock:X8}' was expected to be zero, but was not!");

            this.X = (ushort)(physicalBlock & 0xFFFF);
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.TryIncreasePhysicalBlock"/>
        public override bool TryIncreasePhysicalBlock(bool skipParkingZone = false) {
            if ((this.Y % 2) == 0) {
                if (this.X >= FramesPerTrack - 1) { // End of track.
                    this.Y++;
                } else {
                    this.X++;
                }
            } else if (this.X == 0) { // End of track.
                if (this.Y == TrackCount - 1) { // Last track! (Fast lane)
                    return false;
                } else { // Reached beginning of track, let's move tracks.
                    this.Y++;
                }
            } else {
                this.X--;
            }

            return true;
        }

        /// <inheritdoc cref="OnStreamPhysicalPosition.Clone"/>
        public override OnStreamPhysicalPositionAdr30 Clone() {
            OnStreamPhysicalPositionAdr30 newObj = new OnStreamPhysicalPositionAdr30();
            newObj._x = this._x;
            newObj._y = this._y;
            return newObj;
        }
    }
}