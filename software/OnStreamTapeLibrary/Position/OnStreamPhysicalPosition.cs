using ModToolFramework.Utils.Extensions;
using System;
using System.Data;

namespace OnStreamTapeLibrary.Position
{
    /// <summary>
    /// Represents a physical position on an OnStream tape.
    /// </summary>
    public abstract class OnStreamPhysicalPosition
    {
        /// <summary>
        /// The type of cartridge this position represents.
        /// </summary>
        public readonly OnStreamCartridgeType Type;

        protected OnStreamPhysicalPosition(OnStreamCartridgeType type) {
            this.Type = type;
        }
        
        /// <summary>
        /// The horizontal coordinate of the block on the tape, if the tape were laid out in a straight line.
        /// </summary>
        public abstract ushort X { get; set; }
        
        /// <summary>
        /// The vertical coordinate of the block on the tape, if the tape were laid out in a straight line.
        /// </summary>
        public abstract ushort Y { get; set; }
        
        /// <summary>
        /// Whether the position is representable as a logical block.
        /// </summary>
        public abstract bool IsRepresentableAsLogicalBlock { get; }
        
        /// <summary>
        /// Whether the position is before the parking zone.
        /// </summary>
        public abstract bool IsBeforeParkingZone { get; }
        
        /// <summary>
        /// Whether the position is part of the parking zone.
        /// </summary>
        public abstract bool IsParkingZone { get; }

        /// <summary>
        /// Whether the position is after the parking zone.
        /// </summary>
        public abstract bool IsAfterParkingZone { get; }

        /// <summary>
        /// Gets this position as a logical block string.
        /// This is preferred to accessing the logical block because sometimes it may not be possible to represent the position as a logical block.
        /// </summary>
        /// <returns>logicalBlockString</returns>
        public abstract string ToLogicalBlockString();

        /// <summary>
        /// Gets this position as a physical block string.
        /// </summary>
        /// <returns>physicalBlockString</returns>
        public string ToPhysicalBlockString() {
            return $"{this.ToPhysicalBlock():X8}";
        }

        /// <summary>
        /// Converts the physical position into a logical block number.
        /// </summary>
        /// <returns>logicalBlock</returns>
        /// <exception>Thrown if this position cannot be represented as a logical block.</exception>
        public abstract uint ToLogicalBlock();

        /// <summary>
        /// Converts the physical position into a physical block number for an ADR 50GB tape.
        /// </summary>
        /// <returns>physicalPosNumber</returns>
        public abstract uint ToPhysicalBlock();

        /// <summary>
        /// Loads the position from a logical block.
        /// </summary>
        /// <param name="logicalBlock">The logical block.</param>
        public abstract void FromLogicalBlock(uint logicalBlock);

        /// <summary>
        /// Loads the position from a physical block.
        /// </summary>
        /// <param name="physicalBlock">The physical block.</param>
        public abstract void FromPhysicalBlock(uint physicalBlock);

        /// <summary>
        /// Attempts to increase the physical block by moving horizontally until the end or beginning of tape is reached, where it will switch direction.
        /// For tapes with a parking zone, this becomes a little more complicated because they start in the middle of the tape, so when the end of the tape is reached, it will read from the start, and consider the center of tape "end of data".
        /// </summary>
        /// <param name="skipParkingZone"></param>
        /// <returns>Whether or not the block was increased. If it was not increased, the end of tape has been reached.</returns>
        public abstract bool TryIncreasePhysicalBlock(bool skipParkingZone = false);

        /// <summary>
        /// Attempts to increase the logical block number (The number which data should be addressed with but some software doesn't use).
        /// </summary>
        /// <returns>Whether it was increased or not.</returns>
        public bool TryIncreaseLogicalBlock() {
            return this.TryIncreaseLogicalBlock(out _);
        }

        /// <summary>
        /// Attempts to increase the logical block number (The number which data should be addressed with but some software doesn't use).
        /// </summary>
        /// <returns>Whether it was increased or not.</returns>
        public virtual bool TryIncreaseLogicalBlock(out uint newLogicalPosition) {
            if (!this.IsRepresentableAsLogicalBlock)
                throw new DataException($"This position {this} cannot be represented as a logical block, and can therefore not be increased.");

            uint oldLogicalBlock = this.ToLogicalBlock();
            if (oldLogicalBlock >= this.Type.GetMaxLogicalBlock()) {
                newLogicalPosition = oldLogicalBlock;
                return false;
            }

            newLogicalPosition = oldLogicalBlock + 1;
            this.FromLogicalBlock(newLogicalPosition);
            return true;
        }

        /// <summary>
        /// Creates a copy of this position.
        /// </summary>
        /// <returns>The new copy of this position.</returns>
        public abstract OnStreamPhysicalPosition Clone();

        /// <summary>
        /// Copy another position onto this position.
        /// </summary>
        /// <param name="other">The position to copy from.</param>
        /// <exception cref="Exception">Thrown if the position format is not the same.</exception>
        public void CopyFrom(OnStreamPhysicalPosition other) {
            if (other.Type != this.Type)
                throw new Exception($"Cannot copy position from {other.Type.GetName()} tape position to {this.Type.GetName()} tape position.");
            
            this.FromPhysicalBlock(other.ToPhysicalBlock());
        }
        
        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode() {
            uint hash = this.ToPhysicalBlock();
            hash |= (uint)((int)this.Type << 16);
            return unchecked((int)hash);
        }

        /// <inheritdoc cref="object.Equals(object?)"/>
        public override bool Equals(object? other) {
            if (other is not OnStreamPhysicalPosition physicalPosition)
                return false;

            return physicalPosition.Type == this.Type && this.ToPhysicalBlock() == physicalPosition.ToPhysicalBlock();
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() {
            return $"[{this.Type}/{this.ToPhysicalBlockString()}/{this.ToLogicalBlockString()}]";
        }
    }
}