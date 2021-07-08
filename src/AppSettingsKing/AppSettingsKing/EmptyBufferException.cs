using System;

namespace AppSettingsKing
{
    public class EmptyBufferException : Exception
    {
        public EmptyBufferException()
            : base("You cannot perform operations with an empty buffer.")
        { }
    }
}