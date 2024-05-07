// See https://aka.ms/new-console-template for more information
// This program is used to extract files from raw tape dumps of tapes written with the OnStream SC-X0 drive series using the ARCServe software.
// This software is tested to be compatible with the 50GB tapes, and is potentially compatible with the 30GB tapes, but that has not been tested.
// This software is also tested to be compatible with some non-OnStream tapes too, such as DDS.

using OnStreamSCArcServeExtractor;
using OnStreamTapeLibrary;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

const string defaultConfigFileName = "default-config.txt";

const string programUsage = $@"
ArcServeTapeExtractor:
  Converts an image file from one format to another.
  Author: Kneesnap
  Source Code: https://github.com/Kneesnap/onstream-data-recovery/tree/main/software

Usage:
  ArcServeTapeExtractor [options] <tapeConfigFilePath>
Options:
  --debug          Enable debug logging.
  --fastdebug      Skips slow operations such as file extraction for faster debugging. 

Tape Configurations:
 This program expects you to create a text file containing information about the tape dumps you'd like to extract.
 The default config file has been saved to '{defaultConfigFileName}'. Copy and edit it with your preferred text editor.
 For advanced examples files, refer to the examples included in the source code.
 The '#' character causes further text on the line to be ignored.
 If you need further help, reach out to me either by creating a Github issue or the contact info on my GitHub profile.";

const string defaultConfig = @"type=Raw # The tape cartridge type. In the case of an OnStream 30GB or 50GB tape, use Adr30 and Adr50. Otherwise, use Raw.
name=Whatever I want to call my tape # Optional. This will be the name of the .zip file which gets created.

# OnStream Specific Configuration Keys
#hasAuxData=false # Optional. Only set this if you are 100% certain if OnStream Aux data is present or not.
arcServeSkipExtraFileData=false # If exported files are corrupt, but still look mostly okay, try setting this to true.
#skip=2998,2999 # Optional. This is a list of OnStream logical block IDs which should be skipped.

# The parking zone information can be written here.
# The parking zone only applies to OnStream tape dumps read with an SC-30 or SC-50 drive. (NOT an ADR-50 drive)
# The OnStream data recovery documentation explains this further.
# This section should just be a list of files which only contain parking data.
#
# Example (From Frogger 2's Tape Config):
#parking\tape_01003E43.dump
#parking\tape_02003E3A.dump
#parking\tape_02003E94.dump

# Now it's time to give a list of the tape dump files to read.
# The files will be found in the same directory as the tape config file.
# To add a new tape dump file, the tape dump must have a file name matching 'tape_XXXX.dump'.
# 'XXXX' can be any text of your choosing. For example, if XXXX = 'first', the file name would be 'tape_first.dump'.
# Then, wrap XXXX in square braces like this: [XXXX]. (Example: '[first]' -> would represent 'tape_first.dump').
#[first] # If uncommented, this line would add the file 'tape_first.dump'.


# For troubleshooting or technical support, reach out via the contact info on my Github profile or create a Github issue.
";

// Parse command line arguments.
bool debugMode = false;
string? tapeConfigFilePath = null;
for (int i = 0; i < args.Length; i++)
{
    string argument = args[i];
    if (argument.StartsWith("-", StringComparison.InvariantCultureIgnoreCase))
    {
        string dashedArgument = argument.StartsWith("--", StringComparison.InvariantCultureIgnoreCase)
            ? argument[2..]
            : argument[1..];

        switch (dashedArgument)
        {
            case "debug":
                debugMode = true;
                break;
            case "fastdebug":
                ArcServe.FastDebuggingEnabled = true;
                break;
            default:
                Console.Error.WriteLine($"Unknown command-line option '{dashedArgument}'");
                return 1;
        }

        continue;
    }

    // Read remaining stuff as the file path.
    tapeConfigFilePath = string.Join(" ", args[i..]);
    break;
}

// Show default program usage.
if (string.IsNullOrEmpty(tapeConfigFilePath)) {
    Console.WriteLine(programUsage);
    
    // Write the default config file if it does not exist.
    if (!File.Exists(defaultConfigFileName))
        File.WriteAllText(programUsage, defaultConfig, Encoding.UTF8);

    return 1;
}

// Setup logger for general info. A separate logger will be used for extraction.
using SimpleLogger consoleLogger = new FileLogger(Path.Combine(new FileInfo(tapeConfigFilePath).Directory?.FullName ?? Directory.GetCurrentDirectory(), "tape-info.log"), debugMode, true);
consoleLogger.LogDebug("Parsed Command Line Settings:");
consoleLogger.LogDebug(" - Tape File Path: '{tapeConfigFilePath}'", tapeConfigFilePath);
consoleLogger.LogDebug(" - Debug Mode: {debugMode}", debugMode);
consoleLogger.LogDebug(" - Fast Debug: {fastMode}", ArcServe.FastDebuggingEnabled);

// Read the provided tape config.
TapeDefinition? inputTapeCfg = TapeDefinition.LoadFromConfigFile(tapeConfigFilePath, consoleLogger);
if (inputTapeCfg == null)
    return 1;

// Do something with that config.
ArcServeFileExtractor.ExtractFilesFromTapeDumps(inputTapeCfg, debugMode);

using ZipArchive? archive = ArcServeFileExtractor.OpenExtractedZipArchive(inputTapeCfg, consoleLogger);
if (archive == null)
    return 1;

var results = FileIdentificationScanner.IdentifyFilesInZipFile(archive, consoleLogger);

// Edit results to only show relevant stuff for Frogger2 tape.
/*results.Remove(KnownFileType.TifFile);
results.Remove(KnownFileType.GifFile);
results.Remove(KnownFileType.BmpFile);
results.Remove(KnownFileType.WavFile);
results.Remove(KnownFileType.AviFile);
results.Remove(KnownFileType.TimFile);
results.Remove(KnownFileType.PdfFile);
results.Remove(KnownFileType.JpegFile);
if (results.TryGetValue(KnownFileType.WindowsExe, out var fileList))
    fileList.RemoveAll(entry =>
        entry?.FilePath != null && entry.Value.FilePath.Contains("\\DevStudio\\", StringComparison.InvariantCulture));

results.Remove(KnownFileType.WavFile);*/

// Show Results:
FileIdentificationScanner.LogIdentifiedFiles(consoleLogger, results);
return 0;