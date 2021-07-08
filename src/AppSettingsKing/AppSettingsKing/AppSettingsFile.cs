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

        private readonly List<DataEntry> _entries;
        private readonly IDataFormatter _formatter;
        private readonly Encoding _dataEncoding;

        #region Ctors

        public AppSettingsFile(string fileName)
        {
            // populate public state
            FullFileName = fileName;
            FileName = Path.GetFileName(fileName);

            // populate internal state
            _entries = new List<DataEntry>(InitialSettingsFileEntriesCapacity);
            _formatter = new DefaultDataFormatter();
            _dataEncoding = Encoding.ASCII;
        }

        public AppSettingsFile(string fileName, IDataFormatter dataFormatter)
            : this(fileName)
        {
            // populate internal state
            _formatter = dataFormatter ?? new DefaultDataFormatter();
        }

        public AppSettingsFile(string fileName, IDataFormatter dataFormatter, Encoding dataEncoding)
            : this(fileName, dataFormatter)
        {
            // populate internal state
            _dataEncoding = dataEncoding ?? Encoding.ASCII;
        }

        #endregion

        #region Private methods

        private void _CreateEntryImpl(string entryName, byte[] buffer)
        {
            var entry = new DataEntry(entryName, buffer);
            _entries.Add(entry);
        }

        private byte[] _GetEntryBuffer(string entryName)
        {
            // try get entry
            var entry = _entries.SingleOrDefault(e => e.EntryName == entryName);
            return entry?.Data;
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
            _CreateEntryImpl(entryName, buffer);
        }

        public byte[] GetEntryBuffer(string entryName)
        {
            var buffer = _GetEntryBuffer(entryName);
            return buffer;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
        public void Load()
        {
            // try open file and read data
            using (var fileStream = File.Open(FullFileName, FileMode.Open))
            using (var binaryReader = new BinaryReader(fileStream, _dataEncoding))
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
                    _entries.Add(new DataEntry(entryName, data));
                }
            }
        }

        public void Save()
        {
            // do some validation
            if (_entries.Count == 0)
            {
                throw new InvalidOperationException("Nothing to save.");
            }

            using (var memoryStream = new MemoryStream())
            {
                // write content
                using (var streamWriter = new BinaryWriter(memoryStream, Encoding.ASCII, true))
                {
                    foreach (var dataEntry in _entries)
                    {
                        //var entryNameLength = dataEntry.EntryName.Length;
                        var entryDataLength = dataEntry.Data.Length;

                        // write entry
                        //streamWriter.Write(entryNameLength);
                        streamWriter.Write(dataEntry.EntryName);
                        streamWriter.Write(entryDataLength);
                        streamWriter.Write(dataEntry.Data);
                    }
                }

                using (var fileStream = File.Open(FullFileName, FileMode.OpenOrCreate))
                {
                    // cleanup file content
                    fileStream.SetLength(0);

                    using (var hashCalculator = MD5.Create())
                    {
                        var fileContentHash = hashCalculator.ComputeHash(memoryStream.ToArray());
                        //var fileContentHashLength = BitConverter.GetBytes(fileContentHash.Length);

                        // save in file
                        //fileStream.Write(fileContentHashLength);
                        fileStream.Write(fileContentHash);
                        fileStream.Write(memoryStream.ToArray());
                    }
                }
            }
        }

        #endregion
    }
}