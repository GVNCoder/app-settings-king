namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public class DataEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public string EntryName { get; }
        /// <summary>
        /// 
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Creates an instance of <see cref="DataEntry"/> class
        /// </summary>
        /// <param name="entryName"></param>
        /// <param name="buffer"></param>
        public DataEntry(string entryName, byte[] buffer)
        {
            EntryName = entryName;
            Data = buffer;
        }
    }
}