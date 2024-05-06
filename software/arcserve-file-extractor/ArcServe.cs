using System;
using System.Globalization;

namespace OnStreamSCArcServeExtractor
{
    /// <summary>
    /// Contains general utilities for parsing ArcServe data.
    /// </summary>
    public static class ArcServe
    {
        public const int RootSectorSize = 0x200;
        public static bool FastDebuggingEnabled = false;
        private static readonly Calendar ArcServeCalendar = Calendar.ReadOnly(new GregorianCalendar());

        /// <summary>
        /// Parses an ArcServe timestamp into <see cref="DateTime"/>.
        /// </summary>
        /// <param name="number">The ArcServe timestamp</param>
        /// <param name="startYear">The year which the timestamp counts upwards from</param>
        /// <returns>Parsed timestamp</returns>
        public static DateTime ParseTimeStamp(uint number, int startYear = 1980) {
            if (number == 0)
                return new DateTime(startYear, 1, 1, 0, 0, 0, DateTimeKind.Local);

            uint second = (number & 0b11111) << 1;
            uint minute = (number >> 5) & 0b111111;
            uint hour = ((number >> 11) & 0b11111);
            uint day = (number >> 16) & 0b11111;
            uint month = (number >> 21) & 0b1111;
            uint year = (uint) startYear + ((number >> 25) & 0x7F);
            return new DateTime((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second, DateTimeKind.Local);
        }
        
        /// <summary>
        /// Parses an ArcServe 16-bit date into <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="number">The ArcServe date number</param>
        /// <param name="startYear">The year which the date counts upwards from</param>
        /// <returns>Parsed Date</returns>
        public static DateOnly ParseDate(ushort number, int startYear = 1980) {
            if (number == 0)
                return new DateOnly(startYear, 1, 1, ArcServeCalendar);
            
            int day = (number & 0b11111); // 5 bits
            int month = (number >> 5) & 0b1111; // 4 bits
            int year = startYear + ((number >> 9) & 0x7F); // 7 bits
            return new DateOnly(year, month, day, ArcServeCalendar);
        }

        /// <summary>
        /// Test if the provided string looks valid and is probably not garbage data we read.
        /// </summary>
        /// <param name="input">The string to test.</param>
        /// <param name="allowEmpty">If the string is empty, this will be what the function returns.</param>
        /// <returns>Whether it looks valid</returns>
        public static bool IsValidLookingString(string? input, bool allowEmpty = false) {
            if (string.IsNullOrWhiteSpace(input))
                return allowEmpty;
            
            int validLooking = 0;
            for (int i = 0; i < input.Length; i++) {
                char temp = input[i];
                if ((temp >= 'a' && temp <= 'z') || (temp >= 'A' && temp <= 'Z') || (temp >= '0' && temp <= '9') || temp == '\\' || temp == '_' || temp == '.' || temp == '~' || temp == '-' || temp == '/')
                    validLooking++;
            }

            return input.Length < 20 || validLooking >= input.Length / 2;
        }
    }
}