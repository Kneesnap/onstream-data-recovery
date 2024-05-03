using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using ModToolFramework.Utils;
using ModToolFramework.Utils.Data;

namespace OnStreamTapeLibrary
{
    /// <summary>
    /// This class contains a scanner to scan and identify files and list them.
    /// </summary>
    public static class FileIdentificationScanner
    {
        /// <summary>
        /// Log identified files.
        /// TODO: Read through archive files such as .zip, .7z, .rar, etc to add them too.
        /// </summary>
        /// <param name="logger">The logger to write information to.</param>
        /// <param name="fileEntries">The files to log.</param>
        public static void LogIdentifiedFiles(ILogger logger,
            Dictionary<KnownFileType, List<KnownFileEntry?>> fileEntries)
        {
            logger.LogInformation("Identified File Output:");
            foreach ((KnownFileType fileType, List<KnownFileEntry?> entries) in fileEntries)
            {
                if (entries.Count == 0)
                    continue;

                logger.LogInformation($"{fileType.GetDisplayName()}:");
                foreach (var entry in entries)
                    logger.LogInformation(entry?.ToString() ?? "NULL");
                logger.LogInformation("");
            }
        }

        /// <summary>
        /// Identify files in a zip file.
        /// </summary>
        /// <param name="archive">The archive to identify files from.</param>
        /// <param name="logger">The logger to write information to.</param>
        public static Dictionary<KnownFileType, List<KnownFileEntry?>> IdentifyFilesInZipFile(ZipArchive archive,
            ILogger logger)
        {
            logger.LogInformation("Scanning zip file for known file types...");

            Dictionary<KnownFileType, List<KnownFileEntry?>> results = new();

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Length == 0)
                    continue;
                
                KnownFileEntry fileEntry = new(null, (ulong) entry.Length, entry.Name, entry.FullName, entry.LastWriteTime.DateTime);
                
                using Stream zipStream = entry.Open();
                using CachedReadStream cachedReadStream = new CachedReadStream(zipStream, 0, entry.Length);
                using DataReader reader = new DataReader(cachedReadStream);

                // Run first simple tests for file identification.
                foreach (KnownFileType fileType in Enum.GetValues<KnownFileType>())
                {
                    var fileTypeTester = fileType.GetSimpleTester();
                    reader.Endian = ByteEndian.LittleEndian;
                    if (fileTypeTester != null)
                    {
                        reader.JumpTemp(reader.Index);
                        bool fileTypeMatch = fileTypeTester.Invoke(in fileEntry, reader);
                        reader.JumpReturn();

                        if (fileTypeMatch)
                        {
                            fileEntry.FileType = fileType;
                            break;
                        }
                    }
                }
                
                // The file type was never identified.
                if (!fileEntry.FileType.HasValue)
                    continue;

                // Register entry in list.
                if (!results.TryGetValue(fileEntry.FileType.Value, out var entries))
                    results[fileEntry.FileType.Value] = entries = new List<KnownFileEntry?>();
                entries.Add(fileEntry);
            }

            // Sort each of the lists.
            foreach (var entries in results.Values)
                entries.Sort(Compare);

            return results;
        }
        
        private static int Compare<T>(T? n1, T? n2) where T : struct, IComparable<T?>
        {
            if (n1.HasValue)
            {
                if (n2.HasValue) 
                    return n1.Value.CompareTo(n2);
                return 1;
            }

            return n2.HasValue ? -1 : 0;
        }
    }

    /// <summary>
    /// A struct containing information about a file.
    /// </summary>
    public struct KnownFileEntry : IComparable<KnownFileEntry?>
    {
        /// <summary>
        /// Included if the file type is known / identified.
        /// </summary>
        public KnownFileType? FileType { get; set; }
        
        /// <summary>
        /// The name of the file.
        /// </summary>
        public string? FileName { get; set; }
        
        /// <summary>
        /// The full path to the file.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// The last date the file contents were modified.
        /// </summary>
        public DateTime? LastModification { get; set; }
            
        /// <summary>
        /// The file size in bytes.
        /// </summary>
        public ulong? FileSizeInBytes { get; set; }

        public KnownFileEntry(KnownFileType? fileType, ulong? fileSizeInBytes, string? fileName, string? fullPath = null,
            DateTime? lastModification = null)
        {
            this.FileType = fileType;
            this.LastModification = lastModification;
            this.FileName = fileName;
            this.FilePath = fullPath;
            this.FileSizeInBytes = fileSizeInBytes;
        }

        /// <summary>
        /// Test if the file name / path has the provided extension.
        /// </summary>
        /// <param name="extension">The file extension to test.</param>
        /// <param name="ignoreCase">If the case of the file extension should be ignored. Defaults to true.</param>
        /// <returns>If the entry has the given extension.</returns>
        public readonly bool HasExtension(string extension, bool ignoreCase = true)
        {
            StringComparison comparison =
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
            return (this.FileName != null && this.FileName.EndsWith("." + extension, comparison))
                   || (this.FilePath != null && this.FilePath.EndsWith("." + extension, comparison));
        }
        
        /// <summary>
        /// Appends information about this file to a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="builder">The builder to append the information to.</param>
        public readonly void ToString(StringBuilder builder)
        {
            if (this.LastModification.HasValue)
                builder.AppendFormat(this.LastModification.Value.ToString("yyyy-MM-dd "));

            // Write file size.
            if (this.FileSizeInBytes.HasValue)
            {
                const int intendedArea = 24; // 11 digits for file size, 3 for whitespace and parenthesis, 4 for decimal part of display size number, 3 for size suffix ("Kb", "Mb", etc), 3 for integer part of display size.
                int startLength = builder.Length;
                builder.Append(DataUtils.ConvertByteCountToFileSize(this.FileSizeInBytes.Value, 2));
                builder.Append(" (").Append(this.FileSizeInBytes.Value).Append(')');

                int bytesToAdd = Math.Max(0, intendedArea - (builder.Length - startLength));
                for (int i = 0; i < bytesToAdd; i++)
                    builder.Append(' ');
            }

            // Write file path / name.
            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                builder.Append(this.FilePath);
            }
            else if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                builder.Append(this.FileName);
            }
        }

        /// <inheritdoc cref="ValueType.ToString"/>
        public override readonly string ToString()
        {
            StringBuilder builder = new StringBuilder();
            this.ToString(builder);
            return builder.ToString();
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo"/>
        public readonly int CompareTo(KnownFileEntry? other)
        {
            if (!other.HasValue)
                return (int) ComparableResult.Future;
            
            int typeComparison = Nullable.Compare(this.FileType, other.Value.FileType);
            if (typeComparison != (int) ComparableResult.Current)
                return typeComparison;
            
            int nameComparison = string.Compare(this.FileName, other.Value.FileName, StringComparison.Ordinal);
            if (nameComparison != (int) ComparableResult.Current)
                return nameComparison;

            int dateComparison = Nullable.Compare(this.LastModification, other.Value.LastModification);
            if (dateComparison != (int) ComparableResult.Current)
                return dateComparison;

            int pathComparison = string.Compare(this.FilePath, other.Value.FilePath, StringComparison.Ordinal);
            if (pathComparison != (int) ComparableResult.Current)
                return pathComparison;
            
            return Nullable.Compare(this.FileSizeInBytes, other.Value.FileSizeInBytes);
        }
    }

    /// <summary>
    /// A registry of identifiable file types.
    /// </summary>
    public enum KnownFileType
    {
        // Executables:
        PsxExecutable, // .psx, .exe, .cpe
        PsxSymbolMap, // .sym
        //N64Rom, // .z64, .n64, .bin, etc. Implementing n64 is hard because the easiest way to verify if something is an N64 ROM is to run the checksum algorithm of the ROM, but there can be different checksums for different roms. http://n64dev.org/n64crc.html
        WindowsExe, // .exe, .dll, etc.
        DosExecutable, // .exe, .com?
        MacExecutable, // https://formats.kaitai.io/mach_o/
        ElfBinary, // .elf
        
        // Archives:
        ZipFile, // .zip (.jar, .docx, .xlsx, .pptx)
        RarFile, // .rar
        SevenZipFile, // .7z
        GZipFile, // .gz
        
        // General:
        RtfFile, // .rtf
        PdfFile, // .pdf
        SQLiteDbFile, // .sqlite3
        JavaClassFile, // .class
        MicrosoftOfficeFile, // .ppt, .doc, .xls, .mpp, etc
        
        // Image Files:
        TimFile, // PSX
        PifFile,
        PngFile,
        BmpFile,
        JpegFile,
        GifFile,
        TgaFile,
        TifFile,
        
        // RIFF Files:
        AviFile,
        WavFile,
        RiffFile, // MUST BE THE LAST RIFF FILE.
        
        // TODO: .iso, .img, .mp3, .mov, .mp4
        // TODO: FLA2, OBE, BFF, SPT, 
    }

    /// <summary>
    /// Contains static extension methods for known file types.
    /// </summary>
    public static class KnownFileTypeExtensions
    {
        /// <summary>
        /// A listener for handling the change of an element.
        /// </summary>
        public delegate bool FileTypeSimpleTester(in KnownFileEntry entry, DataReader reader);
        
        /// <summary>
        /// Gets the display name for the file type.
        /// </summary>
        /// <param name="fileType">The file type get a display name for.</param>
        /// <returns>Display name</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the file type is not implemented</exception>
        public static string GetDisplayName(this KnownFileType fileType)
        {
            return fileType switch
            {
                KnownFileType.PsxExecutable => "PlayStation 1 Binary (.CPE, .PSX, .EXE)",
                KnownFileType.PsxSymbolMap => "PlayStation 1 Symbol Map (.SYM)",
                KnownFileType.WindowsExe => "Windows Portable Executable (PE, .EXE, .DLL)",
                KnownFileType.DosExecutable => "Microsoft DOS Executable (.EXE, .COM)",
                KnownFileType.ElfBinary => "ELF Binary (.ELF)",
                KnownFileType.MacExecutable => "Mac OS Binary (Mach O)",
                KnownFileType.ZipFile => "ZIP Archive (.ZIP)",
                KnownFileType.RarFile => "RAR Archive (.RAR)",
                KnownFileType.SevenZipFile => "7-Zip Archive (.7z)",
                KnownFileType.PdfFile => "PDF File (.PDF)",
                KnownFileType.RtfFile => "RTF File (.RTF)",
                KnownFileType.SQLiteDbFile => "SQLite Database",
                KnownFileType.JavaClassFile => "Java Class (Or Apple Mach-O Fat)", // Also could be https://formats.kaitai.io/mach_o_fat/
                KnownFileType.MicrosoftOfficeFile => "Microsoft Office File",
                KnownFileType.GZipFile => "GZip Archive (.gz)",
                KnownFileType.TimFile => "PlayStation Image (.TIM)",
                KnownFileType.PifFile => "Portable Image (.PIF)",
                KnownFileType.PngFile => "PNG Image",
                KnownFileType.BmpFile => "BMP Image",
                KnownFileType.JpegFile => "JPEG Image",
                KnownFileType.GifFile => "GIF Image",
                KnownFileType.TgaFile => "TGA Image",
                KnownFileType.TifFile => "TIF Image",
                KnownFileType.AviFile => "AVI Video",
                KnownFileType.WavFile => "WAV Audio",
                KnownFileType.RiffFile => "RIFF File",
                _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, $"{fileType} has not been implemented in {nameof(KnownFileTypeExtensions)}.{nameof(GetDisplayName)}.")
            };
        }

        /// <summary>
        /// Gets the simple file tester for the file type.
        /// The file tester will test if a file is the provided type.
        /// </summary>
        /// <param name="fileType">The file type to search.</param>
        /// <returns>File tester, or null</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        [SuppressMessage("ReSharper", "ReturnTypeCanBeNotNullable")]
        public static FileTypeSimpleTester? GetSimpleTester(this KnownFileType fileType)
        {
            return fileType switch
            {
                KnownFileType.PsxExecutable => IsPsxExecutable,
                KnownFileType.PsxSymbolMap => IsPsxSymbolMap,
                KnownFileType.WindowsExe => IsWindowsPE,
                KnownFileType.DosExecutable => IsDosExecutable,
                KnownFileType.ElfBinary => IsElfExecutable,
                KnownFileType.MacExecutable => IsMacExecutable,
                KnownFileType.ZipFile => IsZipFile,
                KnownFileType.RarFile => IsRarFile,
                KnownFileType.SevenZipFile => Is7ZipArchive,
                KnownFileType.PdfFile => IsPdfFile,
                KnownFileType.RtfFile => IsRtfFile,
                KnownFileType.SQLiteDbFile => IsSqlite,
                KnownFileType.JavaClassFile => IsJavaClass, // Also could be https://formats.kaitai.io/mach_o_fat/
                KnownFileType.MicrosoftOfficeFile => IsMicrosoftOffice,
                KnownFileType.GZipFile => IsGzipFile,
                KnownFileType.TimFile => IsTimFile,
                KnownFileType.PifFile => IsPifFile,
                KnownFileType.PngFile => IsPngFile,
                KnownFileType.BmpFile => IsBmpFile,
                KnownFileType.JpegFile => IsJpegFile,
                KnownFileType.GifFile => IsGifFile,
                KnownFileType.TgaFile => IsTgaFile,
                KnownFileType.TifFile => IsTifFile,
                KnownFileType.AviFile => IsAviFile,
                KnownFileType.WavFile => IsWavFile,
                KnownFileType.RiffFile => IsRiffFile,
                _ => throw new ArgumentOutOfRangeException(nameof(fileType), fileType, $"{fileType} has not been implemented in {nameof(KnownFileTypeExtensions)}.{nameof(GetSimpleTester)}.")
            };
        }

        private static bool IsPsxExecutable(in KnownFileEntry entry, DataReader reader)
            => reader.TestSignature("CPE\x01") || reader.TestSignature("PS-X EXE");

        private static bool IsPsxSymbolMap(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("MND\x01");
        
        // https://formats.kaitai.io/dos_mz/
        private static bool IsDosExecutable(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("MZ");

        // https://formats.kaitai.io/microsoft_pe/
        // https://learn.microsoft.com/en-us/windows/win32/debug/pe-format
        private static bool IsWindowsPE(in KnownFileEntry entry, DataReader reader)
        {
            if (!reader.TestSignature("MZ"))
                return false;
            
            const int peSignatureOffsetAddress = 0x3C; // From documentation.
            if (peSignatureOffsetAddress + DataConstants.IntegerSize > reader.Size)
                return false; // Not enough data to read the data from.
            
            reader.Index = peSignatureOffsetAddress;
            uint peSignatureOffset = reader.ReadUInt32();
            if (peSignatureOffset + DataConstants.IntegerSize > reader.Size)
                return false; // The data doesn't actually point to somewhere within valid data.

            reader.Index = peSignatureOffset;
            return reader.TestSignature("PE\0\0");
        }

        // https://formats.kaitai.io/mach_o/
        private static bool IsMacExecutable(in KnownFileEntry entry, DataReader reader)
        {
            if (DataConstants.IntegerSize > reader.Remaining)
                return false; // Don't have ID

            uint id = reader.ReadUInt32();
            return id == 0xFEEDFACE || id == 0xCEFAEDFE || id == 0xFEEDFACF || id == 0xCFFAEDFE;
        }
        
        private static bool IsElfExecutable(in KnownFileEntry entry, DataReader reader)
            => reader.TestSignature("\x7F\x45\x4C\x46") && !entry.HasExtension("dwf"); // Don't include dreamcast compiled objects.
        
        // https://formats.kaitai.io/zip/
        private static bool IsZipFile(in KnownFileEntry entry, DataReader reader)
        {
            if (!reader.TestSignature("PK"))
                return false;

            ushort sectionType = reader.ReadUInt16();
            return sectionType == 0x0201 // Central Dir Entity
                   || sectionType == 0x0403 // Local File
                   || sectionType == 0x0605 // End of Central Dir
                   || sectionType == 0x0807; // Data Descriptor
        }
        
        // https://formats.kaitai.io/rar/
        private static bool IsRarFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("Rar!\x1A\x07");
        
        // https://7-zip.org/7z.html
        private static bool Is7ZipArchive(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("7z\xBC\xAF\x27\x1C");
        
        private static bool IsPdfFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("\x25PDF-");
        
        private static bool IsRtfFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("	{\\rtf");

        private static bool IsSqlite(in KnownFileEntry entry, DataReader reader) =>
            reader.TestSignature("SQLite format 3");

        private static bool IsJavaClass(in KnownFileEntry entry, DataReader reader)
        {
            reader.Endian = ByteEndian.BigEndian;
            return reader.Remaining >= DataConstants.IntegerSize && reader.ReadUInt32() == 0xCAFEBABE;
        }

        private static readonly byte[] MicrosoftOfficeSignature = {0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1};
        private static bool IsMicrosoftOffice(in KnownFileEntry entry, DataReader reader)
            => reader.TestSignature(MicrosoftOfficeSignature) && !entry.HasExtension("max");

        // https://formats.kaitai.io/avi/
        private static bool IsAviFile(in KnownFileEntry entry, DataReader reader)
        {
            if (!IsRiffFile(in entry, reader))
                return false;
            
            reader.SkipBytes(DataConstants.IntegerSize);
            return reader.TestSignature("AVI ");
        }

        // https://formats.kaitai.io/wav/
        private static bool IsWavFile(in KnownFileEntry entry, DataReader reader)
        {
            if (!IsRiffFile(in entry, reader))
                return false;
            
            reader.SkipBytes(DataConstants.IntegerSize);
            return reader.TestSignature("WAVE");
        }
        
        // https://formats.kaitai.io/riff/
        private static bool IsRiffFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("RIFF");
       
        // https://formats.kaitai.io/psx_tim/
        // https://fileformats.archiveteam.org/wiki/TIM_(PlayStation_graphics)
        private static bool IsTimFile(in KnownFileEntry entry, DataReader reader)
        {
            if (DataConstants.IntegerSize > reader.Remaining)
                return false;
            
            uint magic = reader.ReadUInt32();
            if (magic != 0x00000010)
                return false; // Wrong magic.

            uint type = reader.ReadUInt32();
            return (type & 0b1011) == type; // Ensure only the bits known to exist by Kaitai struct are set.
        }
        
        // https://formats.kaitai.io/pif/
        private static readonly byte[] PifSignature = { 0x50, 0x49, 0x46, 0x00 }; // 'PIF\0'
        private static bool IsPifFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature(PifSignature);
        
        // https://formats.kaitai.io/png/
        private static readonly byte[] PngSignature = {0x89, 50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A};
        private static bool IsPngFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature(PngSignature);
        
        // https://formats.kaitai.io/bmp/
        private static bool IsBmpFile(in KnownFileEntry entry, DataReader reader)
        {
            if (!reader.TestSignature("BM"))
                return false;

            if (DataConstants.IntegerSize > reader.Remaining)
                return false;

            uint size = reader.ReadUInt32();
            return (size == reader.Size);
        }
        
        // https://formats.kaitai.io/jpeg/
        private static bool IsJpegFile(in KnownFileEntry entry, DataReader reader)
        {
            reader.Endian = ByteEndian.BigEndian;
            byte lastId = 0x00;
            while (reader.HasMore)
            {
                byte currentId = reader.ReadByte();
                if (currentId != 0xFF)
                {
                    return lastId == 0xDA; // Start of scan.
                }

                if (!reader.HasMore)
                    return false; // No more data.

                byte marker = reader.ReadByte();
                if (marker != 0xD8 && marker != 0xD9)
                {
                    ushort offset = reader.ReadUInt16();
                    reader.SkipBytes(offset - DataConstants.ShortSize);
                }
            }

            return true;
        }

        // https://formats.kaitai.io/gif/
        private static bool IsGifFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature("GIF");
        
        // https://formats.kaitai.io/tga/
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static bool IsTgaFile(in KnownFileEntry entry, DataReader reader)
        {
            if (!entry.HasExtension("tga"))
                return false; // This involves reading the end of the file (due to the compressed stream not being possible to seek with), which would cause us to start fully reading most files. Having an extension check will have to do.
            
            const string MagicString = "TRUEVISION-XFILE.\0";
            if (MagicString.Length > reader.Size)
                return false;

            reader.Index = reader.Size - MagicString.Length - 1;
            return reader.TestSignature(MagicString);
        }
        
        private static readonly byte[] TifSignature1 = { 0x49, 0x49, 0x2A, 0x00 };
        private static readonly byte[] TifSignature2 = { 0x4D, 0x4D, 0x00, 0x2A };
        private static bool IsTifFile(in KnownFileEntry entry, DataReader reader)
            => reader.TestSignature(TifSignature1) || reader.TestSignature(TifSignature2);

        // https://formats.kaitai.io/gzip/
        private static readonly byte[] GzipHeader = {0x1F, 0x8B, 0x08};
        private static bool IsGzipFile(in KnownFileEntry entry, DataReader reader) => reader.TestSignature(GzipHeader);
    }
    
    static class DataReaderFileTypeExtensions
    {
        internal static bool TestSignature(this DataReader reader, string signature)
        {
            int byteCount = Encoding.ASCII.GetByteCount(signature);
            Span<byte> bytes = stackalloc byte[byteCount];
            Encoding.ASCII.GetBytes(signature, bytes);
            return TestSignature(reader, bytes);
        }
        
        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        internal static bool TestSignature(this DataReader reader, ReadOnlySpan<byte> signature)
        {
            if (signature.Length == 0)
                return true;

            if (signature.Length > reader.Remaining)
                return false;

            long startIndex = reader.Index;
            for (int i = 0; i < signature.Length; i++)
            {
                if (reader.ReadByte() != signature[i])
                {
                    reader.Index = startIndex; // Go back to start index.
                    return false;
                }
            }
            
            // If we've found it successfully, don't reset the position.
            return true;
        }
    }
}