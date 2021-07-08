using System;

namespace AppSettingsKing
{
    public class InvalidFileHashException : Exception
    {
        public InvalidFileHashException() { }

        public InvalidFileHashException(string sourceFileHash, string realFileHash)
            : base($"Source file hash {sourceFileHash}, real file hash {realFileHash}.")
        {
        }
    }
}