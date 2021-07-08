using System.IO;
using AppSettingsKing;

namespace libTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var settingsFile = new AppSettingsFile("testFile.bruh");

            //using (var dataStream = new MemoryStream())
            //using (var writer = new BinaryWriter(dataStream))
            //{
            //    writer.Write(255);

            //    settingsFile.CreateEntry("bum.txt", dataStream.ToArray());
            //}

            //settingsFile.Save();
            settingsFile.Load();
        }
    }
}
