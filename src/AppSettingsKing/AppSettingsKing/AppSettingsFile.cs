using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertIfStatementToReturnStatement

namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public class AppSettingsFile : ISettingsFile
    {
        #region Constants

        private const int InitialSettingsFileEntriesCapacity = 5;
        private const int FileContentHashSize = 16;

        #endregion

        private List<DataEntry> _entries;

        private readonly IDataFormatter _formatter;
        private readonly IDataFormatter _defaultDataFormatter = new DefaultDataFormatter();

        #region Ctors

        public AppSettingsFile(string fileName)
        {
            // populate public state
            FullFileName = fileName;
            FileName = Path.GetFileName(fileName);

            // populate internal state
            _entries = new List<DataEntry>(InitialSettingsFileEntriesCapacity);
            _formatter = new DefaultDataFormatter();
        }

        public AppSettingsFile(string fileName, IDataFormatter dataFormatter)
            : this(fileName)
        {
            // populate internal state
            _formatter = dataFormatter ?? new DefaultDataFormatter();
        }

        #endregion

        #region Private methods

        private byte[] GenerateFileContent()
        {
            var dataFormatter = _formatter ?? _defaultDataFormatter;

            using (var memoryStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                foreach (var dataEntry in _entries)
                {
                    // preprocess data by formatter
                    var entryData = dataFormatter.Process(dataEntry.Data);
                    var entryDataSize = entryData.Length;

                    // write entry
                    binaryWriter.Write(dataEntry.EntryName);
                    binaryWriter.Write(entryDataSize);
                    binaryWriter.Write(entryData);
                }

                return memoryStream.ToArray();
            }
        }

        #endregion

        #region ISettingsFile impl

        public string FileName { get; }
        public string FullFileName { get; }

        public void CreateEntry(string entryName, byte[] buffer)
        {
            // do some validation
            if (string.IsNullOrWhiteSpace(entryName))
            {
                throw new InvalidEntryNameException(entryName);
            }

            // check is it already contains
            if (_entries.Any(e => e.EntryName == entryName))
            {
                throw new EntryAlreadyContainsException(FileName, entryName);
            }

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // is there some data
            if (buffer.Length == 0)
            {
                throw new EmptyBufferException();
            }

            // ok, we can go forward
            var entry = new DataEntry(entryName, buffer);
            _entries.Add(entry);
        }

        public byte[] GetEntryBuffer(string entryName)
        {
            // try get entry
            var entry = _entries.SingleOrDefault(e => e.EntryName == entryName);
            return entry?.Data;
        }

        public bool RemoveEntry(string entryName)
        {
            // try remove entry
            var entry = _entries.SingleOrDefault(e => e.EntryName == entryName);
            if (entry != null)
            {
                return _entries.Remove(entry);
            }

            return false;
        }

        public IReadOnlyCollection<DataEntry> GetAllEntries()
        {
            return _entries.AsReadOnly();
        }

        public void Load()
        {
            // try open file and read data
            using (var fileStream = File.Open(FullFileName, FileMode.Open))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                // validate file content
                var sourceFileContentHash = binaryReader.ReadBytes(FileContentHashSize);
                var contentPosition = fileStream.Position;

                // compute hash and check file integrity
                using (var md5Hash = MD5.Create())
                {
                    var realFileContentHash = md5Hash.ComputeHash(fileStream);
                    if (realFileContentHash.SequenceEqual(sourceFileContentHash) == false)
                    {
                        var sourceHash = BitConverter.ToString(sourceFileContentHash)
                            .Replace("-", string.Empty)
                            .ToLowerInvariant();
                        var realHash = BitConverter.ToString(realFileContentHash)
                            .Replace("-", string.Empty)
                            .ToLowerInvariant();

                        throw new InvalidFileHashException(sourceHash, realHash);
                    }
                }

                var entries = new List<DataEntry>(InitialSettingsFileEntriesCapacity);

                // ok, lets read file entries
                fileStream.Seek(contentPosition, SeekOrigin.Begin);
                while (binaryReader.PeekChar() != -1)
                {
                    var entryName = binaryReader.ReadString();
                    var dataSize = binaryReader.ReadInt32();
                    var data = binaryReader.ReadBytes(dataSize);

                    // process data by formatter
                    data = _formatter.ProcessBack(data);

                    // create entry
                    entries.Add(new DataEntry(entryName, data));
                }

                // save results
                _entries = entries;
            }
        }

        public void Save()
        {
            // do some validation
            if (_entries.Count == 0)
            {
                throw new InvalidOperationException("Nothing to save.");
            }

            using (var fileStream = File.Open(FullFileName, FileMode.OpenOrCreate))
            {
                // truncate file content
                fileStream.SetLength(0);

                // write file
                using (var md5 = MD5.Create())
                {
                    var fileContentBuffer = GenerateFileContent();
                    var fileContentHash = md5.ComputeHash(fileContentBuffer);

                    // save in file
                    fileStream.Write(fileContentHash);
                    fileStream.Write(fileContentBuffer);
                }
            }
        }

        #endregion
    }
}