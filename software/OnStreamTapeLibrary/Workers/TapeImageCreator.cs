using OnStreamTapeLibrary.Position;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace OnStreamTapeLibrary.Workers
{
    /// <summary>
    /// Creates an image of the tape, creating a visualization of which parts of the tape have been read and which ones have not.
    /// </summary>
    public static class TapeImageCreator
    {
        private const int ImageWidth = OnStreamPhysicalPositionAdr50.BlocksPerTrackSegment;

        /// <summary>
        /// Creates an image which visualizes what parts of the tape have been read / not.
        /// </summary>
        /// <param name="type">The tape cartridge type.</param>
        /// <param name="blockMap">The block map to use to generate the image.</param>
        /// <returns>visualization image</returns>
        public static Image CreateImage(OnStreamCartridgeType type, Dictionary<uint, OnStreamTapeBlock> blockMap) {
            int imageHeight = (int)(GetRowHeight(type) * (1 + (type.GetTrackFrameCount() / ImageWidth)));
            
            Bitmap image = new Bitmap(ImageWidth, imageHeight);

            OnStreamPhysicalPosition pos = type.FromLogicalBlock(0);
            OnStreamPhysicalPosition lastPositionWithData = pos.Clone();
            do {
                uint physicalBlock = pos.ToPhysicalBlock();

                Color color;
                if (blockMap.TryGetValue(physicalBlock, out OnStreamTapeBlock? block)) {
                    if (block.Signature == OnStreamDataStream.WriteStopSignatureNumber) {
                        color = Color.Yellow;
                    } else {
                        color = Color.Green;
                        lastPositionWithData.CopyFrom(pos);
                    }
                } else if (pos.IsParkingZone) {
                    color = Color.Navy;
                } else {
                    color = Color.Red;
                }
                
                GetPixelPosition(pos, out int xPixelPos, out int yPixelPos);
                image.SetPixel(xPixelPos, yPixelPos, color);
            } while (pos.TryIncreasePhysicalBlock());

            // Clear pixels with no data expected in them.
            pos.CopyFrom(lastPositionWithData);
            while (pos.TryIncreasePhysicalBlock()) {
                GetPixelPosition(pos, out int xPixelPos, out int yPixelPos);
                if (image.GetPixel(xPixelPos, yPixelPos).ToArgb() == Color.Red.ToArgb()) 
                    image.SetPixel(xPixelPos, yPixelPos, Color.LightSlateGray);
            }
            
            return image;
        }

        private static void GetPixelPosition(OnStreamPhysicalPosition pos, out int xPos, out int yPos) {
            int globalY;
            if (pos.Type.HasParkingZone() && pos.IsAfterParkingZone) {
                int normalizedX = (pos.X - pos.Type.GetParkingZoneEnd());
                xPos = normalizedX % ImageWidth;
                globalY = (pos.Type.GetParkingZoneEnd() / ImageWidth) + 1 + (normalizedX / ImageWidth);
            } else {
                xPos = pos.X % ImageWidth;
                globalY = pos.X / ImageWidth;
            }

            yPos = (globalY * GetRowHeight(pos.Type)) + pos.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetRowHeight(OnStreamCartridgeType type) => type.GetLogicalTrackCount() + 10; // Add a few pixels for a gap.
    }
}