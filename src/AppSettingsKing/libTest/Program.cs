using System.IO;
using AppSettingsKing;

namespace libTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var settingsFile = new AppSettingsFile("testFile.bruh", new DeflateDataFormatter());

            //settingsFile.AddOrReplaceHeaderServiceData(new byte[] { 0x00 });

            using (var dataStream = new MemoryStream())
            using (var writer = new BinaryWriter(dataStream))
            {
                writer.Write("Some text here bro!");

                settingsFile.CreateEntry("bum.txt", dataStream.ToArray());
            }

            settingsFile.Save();
            settingsFile.Load();
        }
    }
}
