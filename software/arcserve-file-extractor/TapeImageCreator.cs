using System;
using System.Collections.Generic;
using System.Drawing;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// This is used to create a visual reference of the layout of the tape.
    /// </summary>
    public static class TapeImageCreator
    {
        private const int ImageWidth = OnStreamPhysicalPosition.BlocksPerTrackSegment;
        private const int ImageHeight = (OnStreamPhysicalPosition.TrackCount * (1 + (OnStreamPhysicalPosition.FramesPerTrack / ImageWidth)));

        /// <summary>
        /// Creates an image which visualizes what parts of the tape have been read / not.
        /// </summary>
        /// <param name="blockMap">The block map to use to generate the image.</param>
        /// <returns>visualization image</returns>
        public static Image CreateImage(Dictionary<uint, OnStreamTapeBlock> blockMap) {
            Bitmap image = new Bitmap(ImageWidth, ImageHeight);

            OnStreamPhysicalPosition.FromLogicalBlock(0, out OnStreamPhysicalPosition pos);
            OnStreamPhysicalPosition lastPositionWithData = pos;
            do {
                uint physicalBlock = pos.ToPhysicalBlock();

                Color color;
                if (blockMap.ContainsKey(physicalBlock)) {
                    color = Color.Chartreuse;
                    lastPositionWithData = pos;
                } else if (pos.Location == OnStreamTapeAddressableLocation.ParkingZone) {
                    color = Color.Navy;
                } else {
                    color = Color.Maroon;
                }
                
                GetPixelPosition(in pos, out int xPixelPos, out int yPixelPos);
                image.SetPixel(xPixelPos, yPixelPos, color);
            } while (ArcServe.TryIncrementBlockIncludeParkingZone(in pos, out pos));

            // Clear pixels with no data expected in them.
            pos = lastPositionWithData;
            while (ArcServe.TryIncrementBlockIncludeParkingZone(in pos, out pos)) {
                GetPixelPosition(in pos, out int xPixelPos, out int yPixelPos);
                if (image.GetPixel(xPixelPos, yPixelPos).ToArgb() == Color.Maroon.ToArgb()) 
                    image.SetPixel(xPixelPos, yPixelPos, Color.Black);
            }
            
            return image;
        }

        private static void GetPixelPosition(in OnStreamPhysicalPosition pos, out int xPos, out int yPos) {
            int globalY;
            if (pos.Location == OnStreamTapeAddressableLocation.FrontHalf) {
                int normalizedX = (pos.X - OnStreamPhysicalPosition.ParkingZoneEnd);
                xPos = normalizedX % ImageWidth;
                globalY = (OnStreamPhysicalPosition.ParkingZoneEnd / ImageWidth) + 1 + (normalizedX / ImageWidth);
            } else {
                xPos = pos.X % ImageWidth;
                globalY = pos.X / ImageWidth;
            }

            yPos = (globalY * OnStreamPhysicalPosition.TrackCount) + pos.Track;
        }
    }
}