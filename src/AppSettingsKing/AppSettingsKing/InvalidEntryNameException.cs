using System;

namespace AppSettingsKing
{
    public class InvalidEntryNameException : Exception
    {
        public InvalidEntryNameException() { }

        public InvalidEntryNameException(string invalidEntryName)
            : base($"The entry name \"{invalidEntryName}\" is invalid.")
        {
        }
    }
}