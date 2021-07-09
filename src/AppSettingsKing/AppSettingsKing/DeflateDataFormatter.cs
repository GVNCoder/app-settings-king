using System.IO;
using System.IO.Compression;

namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public class DeflateDataFormatter : IDataFormatter
    {
        public byte[] Process(byte[] buffer)
        {
            using (var inputStream = new MemoryStream(buffer))
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(compressor);
                compressor.Flush();

                return compressStream.ToArray();
            }
        }

        public byte[] ProcessBack(byte[] buffer)
        {
            using (var inputStream = new MemoryStream(buffer))
            using (var decompressedStream = new MemoryStream())
            using (var decompressionStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(decompressedStream);
                return decompressedStream.ToArray();
            }
        }
    }
}