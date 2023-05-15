using ModToolFramework.Utils.Extensions;
using System;

namespace OnStreamTapeLibrary.Position
{
    /// <summary>
    /// An exception for when a certain cartridge type does not have certain behavior implemented.
    /// </summary>
    public class OnStreamCartridgeTypeException : Exception
    {
        public OnStreamCartridgeTypeException(OnStreamCartridgeType type, string? message, Exception? innerException)
            : base(GetErrorMessage(type, message), innerException)
        {
        }
        
        public OnStreamCartridgeTypeException(OnStreamCartridgeType type, string? message)
            : base(GetErrorMessage(type, message))
        {
        }
        
        private static string GetErrorMessage(OnStreamCartridgeType type, string? message) {
            return (string.IsNullOrWhiteSpace(message))
                ? $"The {type.GetName()} cartridge type does not have this functionality implemented yet."
                : $"{message} for the {type.GetName()} cartridge type.";
        }
    }
}