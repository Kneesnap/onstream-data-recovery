// See https://aka.ms/new-console-template for more information
// https://opensource.apple.com/source/hfs/hfs-366.1.1/core/hfs_format.h.auto.html <-- this page was a helpful reference when making this.

using System;
using OnStreamTapeLibrary;
using RetrospectTape;

if (args.Length == 0) {
    Console.WriteLine("Usage: Extract.exe <Path to config file>");
    return;
}

string inputFilePath = string.Join(" ", args);

using SimpleLogger consoleLogger = new SimpleLogger();
TapeDefinition? tape = TapeDefinition.LoadFromConfigFile(inputFilePath, consoleLogger);
if (tape == null)
    return;

RetrospectTapeExtractor.ExtractFilesFromTapeDumps(tape);