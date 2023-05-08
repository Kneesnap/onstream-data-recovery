using ModToolFramework.Utils.Data;
using System;
using System.IO;

namespace TestBed
{
    /// <summary>
    /// This code is a port of 'src/compressed/tinf.h' and '/src/compressed/tinflatex.c' as they were found in QuickBMS 0.12.0 on April 15th, 2023.
    /// Those files were a version of "tinf  -  tiny inflate library" by Joergen Ibsen / Jibz, modified by Luigi Auriemma (aluigi) for QuickBMS to support what he calls 'zlibx'.
    /// The file has been ported to C# by Kneesnap in April 2023. The original license has been included below.
    ///
    /// Copyright (c) 2003 by Joergen Ibsen / Jibz
    /// All Rights Reserved
    /// 
    /// http://www.ibsensoftware.com/
    /// 
    /// This software is provided 'as-is', without any express
    /// or implied warranty.  In no event will the authors be
    /// held liable for any damages arising from the use of
    /// this software.
    /// 
    /// Permission is granted to anyone to use this software
    /// for any purpose, including commercial applications,
    /// and to alter it and redistribute it freely, subject to
    /// the following restrictions:
    /// 
    /// 1. The origin of this software must not be
    ///    misrepresented; you must not claim that you
    ///    wrote the original software. If you use this
    ///    software in a product, an acknowledgment in
    ///    the product documentation would be appreciated
    ///    but is not required.
    /// 
    /// 2. Altered source versions must be plainly marked
    ///    as such, and must not be misrepresented as
    ///    being the original software.
    /// 
    /// 3. This notice may not be removed or altered from
    ///    any source distribution.
    /// 
    /// </summary>
    public static class TinyInflate
    {
        private class InflateTree {
            public readonly ushort[] Table = new ushort[16]; // Table of code label counts
            public readonly ushort[] Trans = new ushort[288]; // Code -> symbol translation table
        }

        struct InflateData
        {
            public DataReader ByteReader;
            public BitReader BitReader;
            public DataWriter Writer;
            public MemoryStream WriterOutput;
            public InflateTree LengthTree;
            public InflateTree DistanceTree;
        }

        private const int TableSize = 512;
        private static readonly InflateTree StaticLengthTree = new InflateTree();
        private static readonly InflateTree StaticDistanceTree = new InflateTree();

        private static readonly byte[] LengthBits = new byte[TableSize];
        private static readonly ushort[] LengthBase = new ushort[TableSize];
        private static readonly byte[] DistanceBits = new byte[TableSize];
        private static readonly ushort[] DistanceBase = new ushort[TableSize];

        // Special ordering of code length codes
        private static readonly byte[] ClcIdx = {16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15};

        static TinyInflate() {
            /* build fixed huffman trees */
            BuildFixedTrees(StaticLengthTree, StaticDistanceTree);
            
            /* build extra bits and base tables */
            BuildBitsBase(LengthBits, LengthBase, 4, 3);
            BuildBitsBase(DistanceBits, DistanceBase, 2, 1);

            /* fix a special case */
            LengthBits[28] = 0;
            LengthBase[28] = 258;
        }

        /* build extra bits and base tables */
        private static void BuildBitsBase(byte[] bits, ushort[] baseTable, int delta, int first) {
            /* build bits table */
            Array.Fill(bits, (byte)0, 0, delta);
            for (int i = 0; i < bits.Length - delta; ++i)
                bits[i + delta] = (byte)(i / delta);

                /* build base table */
            int sum = first;
            for (int i = 0; i < baseTable.Length; ++i) {
                baseTable[i] = unchecked((ushort)sum);
                sum += 1 << bits[i];
            }
        }

        /* build the fixed huffman trees */
        private static void BuildFixedTrees(in InflateTree lengthTree, in InflateTree distanceTree) {
            int i;

            /* build fixed length tree */
            Array.Fill(lengthTree.Table, (ushort)0, 0, 7);
            lengthTree.Table[7] = 24;
            lengthTree.Table[8] = 152;
            lengthTree.Table[9] = 112;

            for (i = 0; i < 24; ++i)
                lengthTree.Trans[i] = (ushort)(256 + i);
            for (i = 0; i < 144; ++i)
                lengthTree.Trans[24 + i] = (ushort)i;
            for (i = 0; i < 8; ++i)
                lengthTree.Trans[24 + 144 + i] = (ushort)(280 + i);
            for (i = 0; i < 112; ++i)
                lengthTree.Trans[24 + 144 + 8 + i] = (ushort)(144 + i);

            /* build fixed distance tree */
            Array.Fill(distanceTree.Table, (ushort)0, 0, 5);
            distanceTree.Table[5] = 32;
            for (i = 0; i < 32; ++i)
                distanceTree.Trans[i] = (ushort)i;
        }

        /* given an array of code lengths, build a tree */
        private static void BuildTree(in InflateTree tree, in Span<byte> lengths, uint num, int lenStartIndex) {
            Span<ushort> offs = stackalloc ushort[16];

            /* clear code length count table */
            Array.Fill(tree.Table, (ushort)0, 0, 16);

            /* scan symbol lengths, and sum code length counts */
            for (int i = 0; i < num; ++i)
                tree.Table[lengths[lenStartIndex + i]]++;

            tree.Table[0] = 0;

            /* compute offset table for distribution sort */
            uint sum = 0;
            for (int i = 0; i < 16; ++i) {
                offs[i] = unchecked((ushort)sum);
                sum += tree.Table[i];
            }

            /* create code->symbol translation table (symbols sorted by code) */
            for (int i = 0; i < num; ++i)
                if (lengths[lenStartIndex + i] != 0)
                    tree.Trans[offs[lengths[lenStartIndex + i]]++] = (ushort)i;
        }
        
        /* given a data stream and a tree, decode a symbol */
        private static int DecodeSymbol(in InflateData data, in InflateTree tree) {
            int sum = 0, cur = 0, len = 0;

            /* get more bits while code value is above sum */
            do {
                cur = 2 * cur + data.BitReader.ReadBitAsNumber();

                ++len;

                sum += tree.Table[len];
                cur -= tree.Table[len];
            } while (cur >= 0);

            return tree.Trans[sum + cur];
        }

        /* given a data stream, decode dynamic trees from it */
        static void DecodeTrees(in InflateData data, in InflateTree lengthTree, in InflateTree distanceTree) {
            InflateTree codeTree = new InflateTree();
            Span<byte> lengths = stackalloc byte[288 + 32];

            /* get 5 bits HLIT (257-286) */
            uint hlit = (uint)data.BitReader.ReadBitsAsInteger(5) + 257;
            
            /* get 5 bits HDIST (1-32) */
            uint hdist = (uint)data.BitReader.ReadBitsAsInteger(5) + 1;

            /* get 4 bits HCLEN (4-19) */
            uint hclen = (uint)data.BitReader.ReadBitsAsInteger(4) + 4;

            for (int i = 0; i < 19; ++i)
                lengths[i] = 0;

            /* read code lengths for code length alphabet */
            for (int i = 0; i < hclen; ++i) {
                /* get 3 bits code length (0-7) */
                lengths[ClcIdx[i]] = (byte)data.BitReader.ReadBitsAsInteger(3);
            }

            /* build code length tree */
            BuildTree(in codeTree, lengths, 19, 0);

            /* decode code lengths for the dynamic trees */
            for (int num = 0; num < hlit + hdist;) {
                int sym = DecodeSymbol(in data, in codeTree);

                uint length;
                switch (sym) {
                    case 16:
                        /* copy previous code length 3-6 times (read 2 bits) */
                        byte prev = lengths[num - 1];
                        length = (uint)data.BitReader.ReadBitsAsInteger(2) + 3;
                        while (length-- > 0)
                            lengths[num++] = prev;
                        
                        break;
                    case 17:
                        /* repeat code length 0 for 3-10 times (read 3 bits) */
                        length = (uint)data.BitReader.ReadBitsAsInteger(3) + 3;
                        while (length-- > 0)
                            lengths[num++] = 0;

                        break;
                    case 18:
                        /* repeat code length 0 for 11-138 times (read 7 bits) */
                        length = (uint)data.BitReader.ReadBitsAsInteger(7) + 11;
                        while (length-- > 0)
                            lengths[num++] = 0;

                        break;
                    default:
                        /* values 0-15 represent the actual code lengths */
                        lengths[num++] = unchecked((byte)sym);
                        break;
                }
            }

            /* build dynamic trees */
            BuildTree(in lengthTree, lengths, hlit, 0);
            BuildTree(in distanceTree, lengths, hdist, (int)hlit);
        }

        /* ----------------------------- *
         * -- block inflate functions -- *
         * ----------------------------- */

        /* given a stream and two trees, inflate a block of data */
        private static void InflateBlockData(in InflateData data, in InflateTree lengthTree, in InflateTree distanceTree) {
            /* remember current output position */
            
            while (true) {
                int sym = DecodeSymbol(in data, in lengthTree);

                /* check for end of block */
                if (sym == 256)
                    return;

                if (sym < 256) {
                    data.Writer.Write((byte)sym);
                } else {
                    sym -= 257;

                    /* possibly get more bits from length code */
                    int length = data.BitReader.ReadBitsAsInteger(LengthBits[sym]) + LengthBase[sym];

                    int dist = DecodeSymbol(in data, in distanceTree);

                    /* possibly get more bits from distance code */
                    int offs = data.BitReader.ReadBitsAsInteger(DistanceBits[dist]) + DistanceBase[dist];
                    //Console.WriteLine($"InflateBlockData, sym = {sym}, LengthBits: {LengthBits[sym]}, LengthBase: {LengthBase[sym]}, Length: {length}, Dist: {dist}, DistanceBits: {DistanceBits[dist]}, DistanceBase: {DistanceBase[dist]}, Offset: {offs}");

                    /* copy match */
                    for (int i = 0; i < length; ++i)
                        data.Writer.Write(data.WriterOutput.GetBuffer()[data.WriterOutput.Position - offs]);
                }
            }
        }

        /* inflate an uncompressed block of data */
        private static void InflateUncompressedBlock(in InflateData data) {

            /* get length */
            uint length = data.ByteReader.ReadUInt16();

            /* get one's complement of length */
            uint invLength = data.ByteReader.ReadUInt16();

            /* check length */
            if (length != (~invLength & 0x0000ffff))
                throw new Exception($"Decompression failure in {nameof(InflateUncompressedBlock)}, length/invLength mismatch! (Length: {length}, invLength: {invLength}, Test: {(~invLength & 0x0000ffff)})");

            /* copy block */
            for (uint i = length; i > 0; --i)
                data.Writer.Write(data.ByteReader.ReadByte());
            
            /* make sure we start next block on a byte boundary */
            data.BitReader.SkipRestOfByte();
        }

        /* inflate a block of data compressed with fixed huffman trees */
        private static void InflateFixedBlock(in InflateData data) {
            /* decode block using fixed trees */
            InflateBlockData(in data, in StaticLengthTree, in StaticDistanceTree);
        }

        /* inflate a block of data compressed with dynamic huffman trees */
        private static void InflateDynamicBlock(in InflateData data) {
            /* decode trees from stream */
            DecodeTrees(in data, in data.LengthTree, in data.DistanceTree);

            /* decode block using decoded trees */
            InflateBlockData(in data, in data.LengthTree, in data.DistanceTree);
        }

        /* ---------------------- *
         * -- public functions -- *
         * ---------------------- */

        /* inflate stream from source to dest */
        public static byte[] Uncompress(byte[] sourceData) {

            InflateData data = default;
            data.ByteReader = new DataReader(new MemoryStream(sourceData));
            data.BitReader = new BitReader(data.ByteReader);
            data.BitReader.Mode = BitOrderMode.LowToHigh;
            data.WriterOutput = new MemoryStream();
            using DataWriter byteWriter = new DataWriter(data.WriterOutput);
            data.Writer = byteWriter;
            data.LengthTree = new InflateTree();
            data.DistanceTree = new InflateTree();
            
            bool finalBlock = false;
            while (!finalBlock && data.BitReader.HasMore()) {
                /* read final block flag */
                finalBlock = data.BitReader.ReadBit();

                /* read block type (2 bits) */
                uint btype = (uint)data.BitReader.ReadBitsAsInteger(2);

                /* decompress block */
                if (btype == 0) {
                    /* decompress uncompressed block */
                    InflateUncompressedBlock(in data);
                } else if (btype == 1) {
                    /* decompress block with fixed huffman trees */
                    InflateFixedBlock(in data);
                } else if (btype == 2) {
                    /* decompress block with dynamic huffman trees */
                    InflateDynamicBlock(in data);
                } else {
                    throw new Exception($"Unknown compression block type: {btype}.");
                }
            }
            
            return data.WriterOutput.ToArray();
        }
    }
}