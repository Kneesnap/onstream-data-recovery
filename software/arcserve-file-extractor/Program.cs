// See https://aka.ms/new-console-template for more information
// This program is used to extract files from raw tape dumps of tapes written with the OnStream SC-X0 drive series using the ARCServe software.
// This software is tested to be compatible with the 50GB tapes, and is potentially compatible with the 30GB tapes, but that has not been tested.

using OnStreamSCArcServeExtractor;
using OnStreamTapeLibrary;
using System;
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
TapeDefinition? inputTapeCfg = TapeDefinition.LoadFromConfigFile(inputFilePath, consoleLogger);
if (inputTapeCfg == null)
    return;

// Do something with that config.
ArcServeFileExtractor.ExtractFilesFromTapeDumps(inputTapeCfg);