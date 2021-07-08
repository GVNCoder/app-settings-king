using System;

namespace AppSettingsKing
{
    public class EntryAlreadyContainsException : Exception
    {
        public EntryAlreadyContainsException() { }

        public EntryAlreadyContainsException(string settingsFileName, string entryName)
            : base($"{entryName} already contains in {settingsFileName} file")
        {
        }
    }
}