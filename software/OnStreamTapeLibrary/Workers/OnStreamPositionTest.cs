using Microsoft.Extensions.Logging;
using ModToolFramework.Utils.Extensions;
using OnStreamTapeLibrary.Position;
using System.Collections.Generic;
using System.IO;

namespace OnStreamTapeLibrary.Workers
{
    /// <summary>
    /// Tests that physical positions are working ok.
    /// </summary>
    public static class OnStreamPositionTest
    {
        /// <summary>
        /// Perform tests on all of the supported cartridge type positions.
        /// </summary>
        /// <param name="folder">The folder to run tests in.</param>
        public static void PerformTests(string folder) {
            PerformTest(folder, OnStreamCartridgeType.Adr30);
            PerformTest(folder, OnStreamCartridgeType.Adr50);
        }
        
        /// <summary>
        /// Perform a positioning test.
        /// </summary>
        /// <param name="folder">The folder to write results to.</param>
        /// <param name="type">The tape cartridge type to test.</param>
        public static void PerformTest(string folder, OnStreamCartridgeType type) {
            using FileLogger logger = new FileLogger(Path.Combine(folder, type.GetName() + ".log"), true);

            OnStreamPhysicalPosition position = type.FromLogicalBlock(0);
            OnStreamPhysicalPosition tempPos = type.FromLogicalBlock(0);
            do {
                uint logicalBlock = position.ToLogicalBlock();
                uint physicalBlock = position.ToPhysicalBlock();

                tempPos.FromLogicalBlock(logicalBlock);
                uint logicalBlock2 = tempPos.ToLogicalBlock();
                uint physicalBlock2 = tempPos.ToPhysicalBlock();

                tempPos.FromPhysicalBlock(physicalBlock);
                uint logicalBlock3 = tempPos.ToLogicalBlock();
                uint physicalBlock3 = tempPos.ToPhysicalBlock();

                string pass1Str = (logicalBlock == logicalBlock2) ? "PASS" : "FAIL";
                string pass2Str = (physicalBlock == physicalBlock2) ? "PASS" : "FAIL";
                string pass3Str = (logicalBlock == logicalBlock3) ? "PASS" : "FAIL";
                string pass4Str = (physicalBlock == physicalBlock3) ? "PASS" : "FAIL";
                
                logger.LogInformation($"{logicalBlock}/{physicalBlock:X8}: {logicalBlock2}/{pass1Str}, {physicalBlock2:X8}/{pass2Str}, {logicalBlock3}/{pass3Str}, {physicalBlock3:X8}/{pass4Str}");
            } while (position.TryIncreaseLogicalBlock());
        }
        
        /// <summary>
        /// Write a text file containing the mappings of physical block IDs to logical block IDs.
        /// </summary>
        /// <param name="folder">The folder to write results to.</param>
        /// <param name="type">The tape cartridge type to test.</param>
        public static void LogMappings(string folder, OnStreamCartridgeType type) {
            // Physical to logical.
            OnStreamPhysicalPosition position = type.FromLogicalBlock(0);
            List<string> physicalToLogicalText = new List<string>();
            do {
                physicalToLogicalText.Add($"Physical Position: {position.ToPhysicalBlockString()} -> Logical Position: {position.ToLogicalBlockString()}");
            } while (position.TryIncreasePhysicalBlock());
            File.WriteAllLines(Path.Combine(folder, type.GetName() + "Blocks-PhysicalToLogical.txt"), physicalToLogicalText);

            // Logical to physical.
            position.FromLogicalBlock(0);
            List<string> logicalToPhysicalText = new List<string>();
            do {
                logicalToPhysicalText.Add($"Logical Position: {position.ToLogicalBlockString()} -> Physical Position: {position.ToPhysicalBlockString()}");
            } while (position.TryIncreaseLogicalBlock());
            File.WriteAllLines(Path.Combine(folder, type.GetName() + "Blocks-LogicalToPhysical.txt"), logicalToPhysicalText);
        }
    }
}