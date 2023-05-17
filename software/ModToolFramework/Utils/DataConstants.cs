using System.Diagnostics.CodeAnalysis;

namespace ModToolFramework.Utils
{
    /// <summary>
    /// Contains useful data-related constants.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class DataConstants
    {
        public const int CdSectorSize = 0x800; // 2048.

        public const int ByteSize = 1;
        public const int ShortSize = 2;
        public const int IntegerSize = 4;
        public const int LongSize = 8;
        public const int HalfSize = 2;
        public const int FloatSize = 4;
        public const int DoubleSize = 8;
        public const int DecimalSize = 16;
        
        public const int ByteBitCount = 8;
        public const int ShortBitCount = 16;
        public const int IntegerBitCount = 32;
        public const int LongBitCount = 64;
        public const int HalfBitCount = 16;
        public const int FloatBitCount = 32;
        public const int DoubleBitCount = 64;
        public const int DecimalBitCount = 128;
        
        public const byte NullByte = 0x00;

        public const int BitTrue = 1;
        public const int BitFalse = 0;
        public const int BitsPerByte = 8;

        public const int BitFlag0 = 1 << 0;
        public const int BitFlag1 = 1 << 1;
        public const int BitFlag2 = 1 << 2;
        public const int BitFlag3 = 1 << 3;
        public const int BitFlag4 = 1 << 4;
        public const int BitFlag5 = 1 << 5;
        public const int BitFlag6 = 1 << 6;
        public const int BitFlag7 = 1 << 7;
        public const int BitFlag8 = 1 << 8;
        public const int BitFlag9 = 1 << 9;
        public const int BitFlag10 = 1 << 10;
        public const int BitFlag11 = 1 << 11;
        public const int BitFlag12 = 1 << 12;
        public const int BitFlag13 = 1 << 13;
        public const int BitFlag14 = 1 << 14;
        public const int BitFlag15 = 1 << 15;
        public const int BitFlag16 = 1 << 16;
        public const int BitFlag17 = 1 << 17;
        public const int BitFlag18 = 1 << 18;
        public const int BitFlag19 = 1 << 19;
        public const int BitFlag20 = 1 << 20;
        public const int BitFlag21 = 1 << 21;
        public const int BitFlag22 = 1 << 22;
        public const int BitFlag23 = 1 << 23;
        public const int BitFlag24 = 1 << 24;
        public const int BitFlag25 = 1 << 25;
        public const int BitFlag26 = 1 << 26;
        public const int BitFlag27 = 1 << 27;
        public const int BitFlag28 = 1 << 28;
        public const int BitFlag29 = 1 << 29;
        public const int BitFlag30 = 1 << 30;
        public const int BitFlag31 = 1 << 31;
        public const long BitFlag32 = 1L << 32;
        public const long BitFlag33 = 1L << 33;
        public const long BitFlag34 = 1L << 34;
        public const long BitFlag35 = 1L << 35;
        public const long BitFlag36 = 1L << 36;
        public const long BitFlag37 = 1L << 37;
        public const long BitFlag38 = 1L << 38;
        public const long BitFlag39 = 1L << 39;
        public const long BitFlag40 = 1L << 40;
        public const long BitFlag41 = 1L << 41;
        public const long BitFlag42 = 1L << 42;
        public const long BitFlag43 = 1L << 43;
        public const long BitFlag44 = 1L << 44;
        public const long BitFlag45 = 1L << 45;
        public const long BitFlag46 = 1L << 46;
        public const long BitFlag47 = 1L << 47;
        public const long BitFlag48 = 1L << 48;
        public const long BitFlag49 = 1L << 49;
        public const long BitFlag50 = 1L << 50;
        public const long BitFlag51 = 1L << 51;
        public const long BitFlag52 = 1L << 52;
        public const long BitFlag53 = 1L << 53;
        public const long BitFlag54 = 1L << 54;
        public const long BitFlag55 = 1L << 55;
        public const long BitFlag56 = 1L << 56;
        public const long BitFlag57 = 1L << 57;
        public const long BitFlag58 = 1L << 58;
        public const long BitFlag59 = 1L << 59;
        public const long BitFlag60 = 1L << 60;
        public const long BitFlag61 = 1L << 61;
        public const long BitFlag62 = 1L << 62;
        public const long BitFlag63 = 1L << 63;
    }

    public enum DataUnit
    {
        B,
        Kb,
        Mb,
        Gb, // Gigabyte
        Tb // Terabyte. (I doubt this will ever be used, but it can't hurt to be a little generous.)
        // We can only store 8 Petabytes in a ulong, I'm fine leaving it at terabytes as adding Petabytes would cause overflow a ulong if you tried to test if a value didn't reach the unit after that.
    }
}
