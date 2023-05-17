using System.IO;

namespace ModToolFramework.Utils
{
    // Stolen from https://stackoverflow.com/questions/8128/how-do-i-calculate-crc32-of-a-string
    // Thank you, spludlow.
    public class CRC32
    {
        private readonly uint[] _checksumTable;
        private const uint Polynomial = 0xEDB88320;

        private static readonly CRC32 Instance = new CRC32();

        public CRC32() {
            _checksumTable = new uint[0x100];

            for (uint index = 0; index < 0x100; ++index) {
                uint item = index;
                for (int bit = 0; bit < 8; ++bit)
                    item = ((item & 1) != 0) ? (Polynomial ^ (item >> 1)) : (item >> 1);
                _checksumTable[index] = item;
            }
        }

        public uint ComputeHash(Stream stream) {
            uint result = 0xFFFFFFFF;

            int current;
            while ((current = stream.ReadByte()) != -1)
                result = _checksumTable[(result & 0xFF) ^ (byte)current] ^ (result >> 8);

            return ~result;
        }

        public uint ComputeHash(byte[] data) {
            using MemoryStream stream = new MemoryStream(data);
            return ComputeHash(stream);
        }

        /// <summary>
        /// Computes a CRC32 hash on a byte array of data.
        /// </summary>
        /// <param name="data">The data to hash.</param>
        /// <returns>crcHash</returns>
        public static uint ComputeCRC32Hash(byte[] data) {
            return Instance.ComputeHash(data);
        }
    }
}
