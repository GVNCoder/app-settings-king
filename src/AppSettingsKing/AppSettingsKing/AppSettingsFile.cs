using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;

// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ConvertIfStatementToReturnStatement

namespace AppSettingsKing
{
    /// <summary>
    /// 
    /// </summary>
    public class AppSettingsFile
    {
        #region Constants

        private const int InitialSettingsFileEntriesCapacity = 8;
        private const int FileContentHashSize = 16;

        #endregion

        private readonly byte[] _emptyBuffer = Array.Empty<byte>();

        private readonly IDataFormatter _dataFormatter;
        private readonly CreateDataFormatterAction _createDataFormatterAction;

        private List<DataEntry> _entries;
        private byte[] _headerServiceData;

        #region Ctors

        public AppSettingsFile(string fileName)
        {
            // populate public state
            FullFileName = fileName;
            FileName = Path.GetFileName(fileName);

            // populate internal state
            _entries = new List<DataEntry>(InitialSettingsFileEntriesCapacity);
        }

        public AppSettingsFile(string fileName, IDataFormatter dataFormatter)
            : this(fileName)
        {
            // populate internal state
            _dataFormatter = dataFormatter;
        }

        public AppSettingsFile(string fileName, CreateDataFormatterAction createDataFormatterAction)
            : this(fileName)
        {
            // populate internal state
            _createDataFormatterAction = createDataFormatterAction;
        }

        #endregion

        #region Private methods

        private byte[] GenerateFileContent()
        {
            using (var memoryStream = new MemoryStream())
            using (var binaryWriter = new BinaryWriter(memoryStream))
            {
                foreach (var dataEntry in _entries)
                {
                    // preprocess data by formatter
                    var entryData = dataEntry.Data;
                    var entryDataSize = entryData.Length;

                    // write entry
                    binaryWriter.Write(dataEntry.EntryName);
                    binaryWriter.Write(entryDataSize);
                    binaryWriter.Write(entryData);
                }

                return memoryStream.ToArray();
            }
        }

        private static FileHeaderData ReadFileHeader(BinaryReader binaryReader)
        {
            var contentHash = binaryReader.ReadBytes(FileContentHashSize);
            var serviceDataSize = binaryReader.ReadInt32();
            var serviceData = binaryReader.ReadBytes(serviceDataSize);

            return new FileHeaderData { ContentHash = contentHash, ServiceData = serviceData };
        }

        private static void ThrowIfFileHashNotEqual(byte[] content, FileHeaderData fileHeader)
        {
            using (var md5Hash = MD5.Create())
            {
                var realFileContentHash = md5Hash.ComputeHash(content);
                if (realFileContentHash.SequenceEqual(fileHeader.ContentHash) == false)
                {
                    var sourceHash = BitConverter.ToString(fileHeader.ContentHash)
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();
                    var realHash = BitConverter.ToString(realFileContentHash)
                        .Replace("-", string.Empty)
                        .ToLowerInvariant();

                    throw new InvalidFileHashException(sourceHash, realHash);
                }
            }
        }

        private IDataFormatter CreateDataFormatter(byte[] serviceData)
        {
            var dataFormatter = _createDataFormatterAction?.Invoke(serviceData);
            return dataFormatter ?? _dataFormatter ?? new DefaultDataFormatter();
        }

        private static List<DataEntry> ParseDataEntries(byte[] content)
        {
            var entries = new List<DataEntry>(InitialSettingsFileEntriesCapacity);

            using (var contentMemoryStream = new MemoryStream(content))
            using (var contentBinaryReader = new BinaryReader(contentMemoryStream))
            {
                // ok, lets read file entries
                while (contentBinaryReader.PeekChar() != -1)
                {
                    var entryName = contentBinaryReader.ReadString();
                    var dataSize = contentBinaryReader.ReadInt32();
                    var data = contentBinaryReader.ReadBytes(dataSize);

                    // create entry
                    entries.Add(new DataEntry(entryName, data));
                }
            }

            return entries;
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

        public void AddOrReplaceHeaderServiceData(byte[] serviceData)
        {
            // do some validation
            if (serviceData == null)
            {
                throw new ArgumentNullException(nameof(serviceData));
            }

            if (serviceData.Length == 0)
            {
                throw new EmptyBufferException();
            }

            _headerServiceData = serviceData;
        }

        public void Load()
        {
            using (var fileStream = File.Open(FullFileName, FileMode.Open))
            using (var binaryReader = new BinaryReader(fileStream))
            {
                // read header and file content
                var fileHeader = ReadFileHeader(binaryReader);
                var content = binaryReader.ReadBytes((int) fileStream.CountAvailableBytes());

                // validate file content
                // compute hash and check file integrity
                ThrowIfFileHashNotEqual(content, fileHeader);

                var dataFormatter = CreateDataFormatter(fileHeader.ServiceData);
                var originalContent = dataFormatter.ProcessBack(content);

                _entries = ParseDataEntries(originalContent);
            }
        }

        public void Save()
        {
            // do some validation
            if (_entries.Count == 0)
            {
                throw new InvalidOperationException("Nothing to save.");
            }

            var dataFormatter = CreateDataFormatter(_headerServiceData);
            var content = GenerateFileContent();

            // process data
            var formattedContent = dataFormatter.Process(content);
            var serviceData = _headerServiceData ?? _emptyBuffer;

            byte[] contentHash;

            using (var md5 = MD5.Create())
            {
                contentHash = md5.ComputeHash(formattedContent);
            }

            using (var fileStream = File.Open(FullFileName, FileMode.OpenOrCreate))
            {
                // truncate file content
                fileStream.SetLength(0);

                using (var binaryWriter = new BinaryWriter(fileStream))
                {
                    // write header
                    binaryWriter.Write(contentHash);
                    binaryWriter.Write(serviceData.Length);
                    binaryWriter.Write(serviceData);

                    // write content
                    binaryWriter.Write(formattedContent);
                }
            }
        }

        #endregion
    }
}