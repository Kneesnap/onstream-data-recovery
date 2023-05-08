// See https://aka.ms/new-console-template for more information
// This program is used to extract files from raw tape dumps of tapes written with the OnStream SC-X0 drive series using the ARCServe software.
// This software is currently only compatible with the 50GB tapes.
// 30GB tapes would require modification to this program (Specifically OnStreamPhysicalPosition.cs would need to be changed to meet the physical differences between the 30GB and 50GB layout)
// Tapes written with an ADR-X0 tape drive should use the other extractor.

using OnStreamSCArcServeExtractor;
using System.IO;
using System.IO.Compression;
using System.Text;

if (args.Length == 0) {
    Console.WriteLine("Usage: Extract.exe <Path to tape config>");
    return;
}

string inputFilePath = string.Join(" ", args);

// Read the provided tape config.
using SimpleLogger consoleLogger = new SimpleLogger();
using TapeConfig? inputTapeCfg = TapeConfig.LoadFromConfigFile(inputFilePath, consoleLogger);
if (inputTapeCfg == null)
    return;

// Do something with that config.
ArcServeFileExtractor.ExtractFilesFromTapeDumps(inputTapeCfg);