//
// MacRoman.cs
//
// Author:
//       Natalia Portillo <claunia@claunia.com>
//
// Copyright © 2016-2021 Natalia Portillo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace RetrospectTape.MacEncoding
{
    /// <summary>Represents an Apple Mac character encoding of Unicode characters.</summary>
    public class MacRoman : SingleByteEncoding
    {
        /// <inheritdoc />
        public override string BodyName => "macintosh";

        /// <inheritdoc />
        public override int CodePage => 10000;

        /// <inheritdoc />
        public override string EncodingName => "Western European (Mac)";

        /// <inheritdoc />
        public override string HeaderName => "macintosh";

        /// <inheritdoc />
        public override string WebName => "macintosh";

        /// <inheritdoc />
        public override int WindowsCodePage => 10000;

        /// <inheritdoc />
        public override bool IsBrowserDisplay => false;

        /// <inheritdoc />
        public override bool IsBrowserSave => false;

        /// <inheritdoc />
        public override bool IsMailNewsDisplay => false;

        /// <inheritdoc />
        public override bool IsMailNewsSave => false;

        /// <inheritdoc />
        public override bool IsSingleByte => true;

        /// <summary>The Mac to Unicode character map. MacRoman is a superset of LisaRoman.</summary>
        protected override char[] CharTable => new[] {
            // 0x00
            '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007',

            // 0x08
            '\u0008', '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u000E', '\u000F',

            // 0x10
            '\u0010', '\u2318', '\u2713', '\u25C6', '\uF8FF', '\u0015', '\u0016', '\u0017',

            // 0x18
            '\u0018', '\u0019', '\u001A', '\u001B', '\u001C', '\u001D', '\u001E', '\u001F',

            // 0x20
            '\u0020', '\u0021', '\u0022', '\u0023', '\u0024', '\u0025', '\u0026', '\u0027',

            // 0x28
            '\u0028', '\u0029', '\u002A', '\u002B', '\u002C', '\u002D', '\u002E', '\u002F',

            // 0x30
            '\u0030', '\u0031', '\u0032', '\u0033', '\u0034', '\u0035', '\u0036', '\u0037',

            // 0x38
            '\u0038', '\u0039', '\u003A', '\u003B', '\u003C', '\u003D', '\u003E', '\u003F',

            // 0x40
            '\u0040', '\u0041', '\u0042', '\u0043', '\u0044', '\u0045', '\u0046', '\u0047',

            // 0x48
            '\u0048', '\u0049', '\u004A', '\u004B', '\u004C', '\u004D', '\u004E', '\u004F',

            // 0x50
            '\u0050', '\u0051', '\u0052', '\u0053', '\u0054', '\u0055', '\u0056', '\u0057',

            // 0x58
            '\u0058', '\u0059', '\u005A', '\u005B', '\u005C', '\u005D', '\u005E', '\u005F',

            // 0x60
            '\u0060', '\u0061', '\u0062', '\u0063', '\u0064', '\u0065', '\u0066', '\u0067',

            // 0x68
            '\u0068', '\u0069', '\u006A', '\u006B', '\u006C', '\u006D', '\u006E', '\u006F',

            // 0x70
            '\u0070', '\u0071', '\u0072', '\u0073', '\u0074', '\u0075', '\u0076', '\u0077',

            // 0x78
            '\u0078', '\u0079', '\u007A', '\u007B', '\u007C', '\u007D', '\u007E', '\u007F',

            // 0x80
            '\u00C4', '\u00C5', '\u00C7', '\u00C9', '\u00D1', '\u00D6', '\u00DC', '\u00E1',

            // 0x88
            '\u00E0', '\u00E2', '\u00E4', '\u00E3', '\u00E5', '\u00E7', '\u00E9', '\u00E8',

            // 0x90
            '\u00EA', '\u00EB', '\u00ED', '\u00EC', '\u00EE', '\u00EF', '\u00F1', '\u00F3',

            // 0x98
            '\u00F2', '\u00F4', '\u00F6', '\u00F5', '\u00FA', '\u00F9', '\u00FB', '\u00FC',

            // 0xA0
            '\u2020', '\u00B0', '\u00A2', '\u00A3', '\u00A7', '\u2022', '\u00B6', '\u00DF',

            // 0xA8
            '\u00AE', '\u00A9', '\u2122', '\u00B4', '\u00A8', '\u2260', '\u00C6', '\u00D8',

            // 0xB0
            '\u221E', '\u00B1', '\u2264', '\u2265', '\u00A5', '\u00B5', '\u2202', '\u2211',

            // 0xB8
            '\u220F', '\u03C0', '\u222B', '\u00AA', '\u00BA', '\u03A9', '\u00E6', '\u00F8',

            // 0xC0
            '\u00BF', '\u00A1', '\u00AC', '\u221A', '\u0192', '\u2248', '\u2206', '\u00AB',

            // 0xC8
            '\u00BB', '\u2026', '\u00A0', '\u00C0', '\u00C3', '\u00D5', '\u0152', '\u0153',

            // 0xD0
            '\u2013', '\u2014', '\u201C', '\u201D', '\u2018', '\u2019', '\u00F7', '\u25CA',

            // 0xD8
            '\u00FF', '\u0178', '\u2044', '\u20AC', '\u2039', '\u203A', '\uFB01', '\uFB02',

            // 0xE0
            '\u2021', '\u00B7', '\u201A', '\u201E', '\u2030', '\u00C2', '\u00CA', '\u00C1',

            // 0xE8
            '\u00CB', '\u00C8', '\u00CD', '\u00CE', '\u00CF', '\u00CC', '\u00D3', '\u00D4',

            // 0xF0
            '\uF8FF', '\u00D2', '\u00DA', '\u00DB', '\u00D9', '\u0131', '\u02C6', '\u02DC',

            // 0xF8
            '\u00AF', '\u02D8', '\u02D9', '\u02DA', '\u00B8', '\u02DD', '\u02DB', '\u02C7'
        };

        public static readonly MacRoman Instance = new MacRoman();

        /// <summary>Converts a Unicode character to an MacRoman character</summary>
        /// <returns>MacRoman character.</returns>
        /// <param name="character">Unicode character.</param>
        private protected override byte GetByte(char character) => character switch {
            '\u0000' => 0x00,
            '\u0001' => 0x01,
            '\u0002' => 0x02,
            '\u0003' => 0x03,
            '\u0004' => 0x04,
            '\u0005' => 0x05,
            '\u0006' => 0x06,
            '\u0007' => 0x07,
            '\u0008' => 0x08,
            '\u0009' => 0x09,
            '\u000A' => 0x0A,
            '\u000B' => 0x0B,
            '\u000C' => 0x0C,
            '\u000D' => 0x0D,
            '\u000E' => 0x0E,
            '\u000F' => 0x0F,
            '\u0010' => 0x10,
            '\u0011' => 0x11,
            '\u2318' => 0x11,
            '\u0012' => 0x12,
            '\u2713' => 0x12,
            '\u0013' => 0x13,
            '\u25C6' => 0x13,
            '\u0014' => 0x14,
            '\u0015' => 0x15,
            '\u0016' => 0x16,
            '\u0017' => 0x17,
            '\u0018' => 0x18,
            '\u0019' => 0x19,
            '\u001A' => 0x1A,
            '\u001B' => 0x1B,
            '\u001C' => 0x1C,
            '\u001D' => 0x1D,
            '\u001E' => 0x1E,
            '\u001F' => 0x1F,
            '\u0020' => 0x20,
            '\u0021' => 0x21,
            '\u0022' => 0x22,
            '\u0023' => 0x23,
            '\u0024' => 0x24,
            '\u0025' => 0x25,
            '\u0026' => 0x26,
            '\u0027' => 0x27,
            '\u0028' => 0x28,
            '\u0029' => 0x29,
            '\u002A' => 0x2A,
            '\u002B' => 0x2B,
            '\u002C' => 0x2C,
            '\u002D' => 0x2D,
            '\u002E' => 0x2E,
            '\u002F' => 0x2F,
            '\u0030' => 0x30,
            '\u0031' => 0x31,
            '\u0032' => 0x32,
            '\u0033' => 0x33,
            '\u0034' => 0x34,
            '\u0035' => 0x35,
            '\u0036' => 0x36,
            '\u0037' => 0x37,
            '\u0038' => 0x38,
            '\u0039' => 0x39,
            '\u003A' => 0x3A,
            '\u003B' => 0x3B,
            '\u003C' => 0x3C,
            '\u003D' => 0x3D,
            '\u003E' => 0x3E,
            '\u003F' => 0x3F,
            '\u0040' => 0x40,
            '\u0041' => 0x41,
            '\u0042' => 0x42,
            '\u0043' => 0x43,
            '\u0044' => 0x44,
            '\u0045' => 0x45,
            '\u0046' => 0x46,
            '\u0047' => 0x47,
            '\u0048' => 0x48,
            '\u0049' => 0x49,
            '\u004A' => 0x4A,
            '\u004B' => 0x4B,
            '\u004C' => 0x4C,
            '\u004D' => 0x4D,
            '\u004E' => 0x4E,
            '\u004F' => 0x4F,
            '\u0050' => 0x50,
            '\u0051' => 0x51,
            '\u0052' => 0x52,
            '\u0053' => 0x53,
            '\u0054' => 0x54,
            '\u0055' => 0x55,
            '\u0056' => 0x56,
            '\u0057' => 0x57,
            '\u0058' => 0x58,
            '\u0059' => 0x59,
            '\u005A' => 0x5A,
            '\u005B' => 0x5B,
            '\u005C' => 0x5C,
            '\u005D' => 0x5D,
            '\u005E' => 0x5E,
            '\u005F' => 0x5F,
            '\u0060' => 0x60,
            '\u0061' => 0x61,
            '\u0062' => 0x62,
            '\u0063' => 0x63,
            '\u0064' => 0x64,
            '\u0065' => 0x65,
            '\u0066' => 0x66,
            '\u0067' => 0x67,
            '\u0068' => 0x68,
            '\u0069' => 0x69,
            '\u006A' => 0x6A,
            '\u006B' => 0x6B,
            '\u006C' => 0x6C,
            '\u006D' => 0x6D,
            '\u006E' => 0x6E,
            '\u006F' => 0x6F,
            '\u0070' => 0x70,
            '\u0071' => 0x71,
            '\u0072' => 0x72,
            '\u0073' => 0x73,
            '\u0074' => 0x74,
            '\u0075' => 0x75,
            '\u0076' => 0x76,
            '\u0077' => 0x77,
            '\u0078' => 0x78,
            '\u0079' => 0x79,
            '\u007A' => 0x7A,
            '\u007B' => 0x7B,
            '\u007C' => 0x7C,
            '\u007D' => 0x7D,
            '\u007E' => 0x7E,
            '\u007F' => 0x7F,
            '\u00C4' => 0x80,
            '\u00C5' => 0x81,
            '\u00C7' => 0x82,
            '\u00C9' => 0x83,
            '\u00D1' => 0x84,
            '\u00D6' => 0x85,
            '\u00DC' => 0x86,
            '\u00E1' => 0x87,
            '\u00E0' => 0x88,
            '\u00E2' => 0x89,
            '\u00E4' => 0x8A,
            '\u00E3' => 0x8B,
            '\u00E5' => 0x8C,
            '\u00E7' => 0x8D,
            '\u00E9' => 0x8E,
            '\u00E8' => 0x8F,
            '\u00EA' => 0x90,
            '\u00EB' => 0x91,
            '\u00ED' => 0x92,
            '\u00EC' => 0x93,
            '\u00EE' => 0x94,
            '\u00EF' => 0x95,
            '\u00F1' => 0x96,
            '\u00F3' => 0x97,
            '\u00F2' => 0x98,
            '\u00F4' => 0x99,
            '\u00F6' => 0x9A,
            '\u00F5' => 0x9B,
            '\u00FA' => 0x9C,
            '\u00F9' => 0x9D,
            '\u00FB' => 0x9E,
            '\u00FC' => 0x9F,
            '\u2020' => 0xA0,
            '\u00B0' => 0xA1,
            '\u00A2' => 0xA2,
            '\u00A3' => 0xA3,
            '\u00A7' => 0xA4,
            '\u2022' => 0xA5,
            '\u00B6' => 0xA6,
            '\u00DF' => 0xA7,
            '\u00AE' => 0xA8,
            '\u00A9' => 0xA9,
            '\u2122' => 0xAA,
            '\u00B4' => 0xAB,
            '\u00A8' => 0xAC,
            '\u2260' => 0xAD,
            '\u00C6' => 0xAE,
            '\u00D8' => 0xAF,
            '\u221E' => 0xB0,
            '\u00B1' => 0xB1,
            '\u2264' => 0xB2,
            '\u2265' => 0xB3,
            '\u00A5' => 0xB4,
            '\u00B5' => 0xB5,
            '\u2202' => 0xB6,
            '\u2211' => 0xB7,
            '\u220F' => 0xB8,
            '\u03C0' => 0xB9,
            '\u222B' => 0xBA,
            '\u00AA' => 0xBB,
            '\u00BA' => 0xBC,
            '\u03A9' => 0xBD,
            '\u00E6' => 0xBE,
            '\u00F8' => 0xBF,
            '\u00BF' => 0xC0,
            '\u00A1' => 0xC1,
            '\u00AC' => 0xC2,
            '\u221A' => 0xC3,
            '\u0192' => 0xC4,
            '\u2248' => 0xC5,
            '\u2206' => 0xC6,
            '\u00AB' => 0xC7,
            '\u00BB' => 0xC8,
            '\u2026' => 0xC9,
            '\u00A0' => 0xCA,
            '\u00C0' => 0xCB,
            '\u00C3' => 0xCC,
            '\u00D5' => 0xCD,
            '\u0152' => 0xCE,
            '\u0153' => 0xCF,
            '\u2013' => 0xD0,
            '\u2014' => 0xD1,
            '\u201C' => 0xD2,
            '\u201D' => 0xD3,
            '\u2018' => 0xD4,
            '\u2019' => 0xD5,
            '\u00F7' => 0xD6,
            '\u25CA' => 0xD7,
            '\u00FF' => 0xD8,
            '\u0178' => 0xD9,
            '\u2044' => 0xDA,
            '\u20AC' => 0xDB,
            '\u2039' => 0xDC,
            '\u203A' => 0xDD,
            '\uFB01' => 0xDE,
            '\uFB02' => 0xDF,
            '\u2021' => 0xE0,
            '\u00B7' => 0xE1,
            '\u201A' => 0xE2,
            '\u201E' => 0xE3,
            '\u2030' => 0xE4,
            '\u00C2' => 0xE5,
            '\u00CA' => 0xE6,
            '\u00C1' => 0xE7,
            '\u00CB' => 0xE8,
            '\u00C8' => 0xE9,
            '\u00CD' => 0xEA,
            '\u00CE' => 0xEB,
            '\u00CF' => 0xEC,
            '\u00CC' => 0xED,
            '\u00D3' => 0xEE,
            '\u00D4' => 0xEF,
            '\uF8FF' => 0xF0,
            '\u00D2' => 0xF1,
            '\u00DA' => 0xF2,
            '\u00DB' => 0xF3,
            '\u00D9' => 0xF4,
            '\u0131' => 0xF5,
            '\u02C6' => 0xF6,
            '\u02DC' => 0xF7,
            '\u00AF' => 0xF8,
            '\u02D8' => 0xF9,
            '\u02D9' => 0xFA,
            '\u02DA' => 0xFB,
            '\u00B8' => 0xFC,
            '\u02DD' => 0xFD,
            '\u02DB' => 0xFE,
            '\u02C7' => 0xFF,
            _ => 0x3F
        };
    }
}