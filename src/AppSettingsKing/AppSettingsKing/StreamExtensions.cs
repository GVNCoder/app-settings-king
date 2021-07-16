using System.IO;

// ReSharper disable MemberCanBePrivate.Global

namespace AppSettingsKing
{
    internal static class StreamExtensions
    {
        public static long CountAvailableBytes(this Stream stream)
        {
            return stream.Length - stream.Position;
        }
    }
}