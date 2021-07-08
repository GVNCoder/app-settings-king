using System.Collections.Generic;

namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public interface ISettingsFile
    {
        /// <summary>
        /// 
        /// </summary>
        string FileName { get; }
        /// <summary>
        /// 
        /// </summary>
        string FullFileName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entryName"></param>
        /// <param name="buffer"></param>
        void CreateEntry(string entryName, byte[] buffer);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entryName"></param>
        /// <returns></returns>
        byte[] GetEntryBuffer(string entryName);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entryName"></param>
        /// <returns></returns>
        bool RemoveEntry(string entryName);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<DataEntry> GetAllEntries();

        /// <summary>
        /// 
        /// </summary>
        void Save();
        /// <summary>
        /// 
        /// </summary>
        void Load();
    }
}