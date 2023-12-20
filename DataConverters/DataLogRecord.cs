using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommunityToolkit.HighPerformance.Streams;

namespace Titan.DataConverters
{
    public class DataLogRecord : IDisposable
    {
        private const int kControlStart = 0;
        private const int kControlFinish = 1;
        private const int kControlSetMetadata = 2;

        public int Entry { get; private set; } = 0;
        public long Timestamp { get; private set; } = 0;
        public int Size { get => Data.Length; }

        private ReadOnlyMemory<byte> Data;

        private MemoryStream ReadData;

        public DataLogRecord(int entry, long timestamp, ReadOnlyMemory<byte> data)
        {
            Entry = entry;
            Timestamp = timestamp;
            Data = data;
            ReadData = new MemoryStream(data.ToArray());
        }

        /// <summary>
        /// Returns true if the record is a control record.
        /// </summary>
        /// <returns>True if control record, false if normal record</returns>
        public bool IsControl()
        {
            return Entry == 0;
        }

        /// <summary>
        /// Returns true if the record is a start control record. Use
        /// GetStartData() to decode the contents.
        /// </summary>
        /// <returns>True if start control record, false otherwise</returns>
        public bool IsStart()
        {
            return Entry == 0 && Data.Length >= 16 && Data.Span[0] == kControlStart;
        }

        /// <summary>
        /// Returns true if the record is a finish control record. Use GetFinishEntry()
        /// to decode the contents.
        /// </summary>
        /// <returns>True if finish control record, false otherwise</returns>
        public bool IsFinish()
        {
            return Entry == 0 && Data.Length >= 5 && Data.Span[0] == kControlFinish;
        }

        /// <summary>
        /// Returns true if the record is a set metadata control record.
        /// Use GetSetMetadataData() to decode the contents.
        /// </summary>
        /// <returns>True if set metadata control record, false otherwise</returns>
        public bool IsSetMetadata()
        {
            return Entry == 0 && Data.Length >= 9 && Data.Span[0] == kControlSetMetadata;
        }

        /// <summary>
        /// Recodes a start control record
        /// </summary>
        /// <returns>Decoded StartRecordData</returns>
        /// <exception cref="InvalidOperationException">On incorrect record type.</exception>
        public StartRecordData GetStartData()
        {
            if (!IsStart())
            {
                throw new InvalidOperationException($"Called {nameof(GetStartData)} on an incorrect record type.");
            }

            using var stream = new MemoryStream(Data.ToArray());

            stream.Position = 1; // skip over control type
            int entry = stream.ReadInt();
            string name = stream.ReadInnerString();
            string type = stream.ReadInnerString();
            string metadata = stream.ReadInnerString();

            return new StartRecordData(entry, name, type, metadata);
        }

        public MetadataRecordData GetSetMetadataData()
        {
            if (!IsSetMetadata())
            {
                throw new InvalidOperationException($"Called {nameof(GetSetMetadataData)} on an incorrect record type.");
            }

            using var stream = new MemoryStream(Data.ToArray());

            stream.Position = 1; // skip over control type
            int entry = stream.ReadInt();
            string metadata = stream.ReadInnerString();

            return new MetadataRecordData(entry, metadata);
        }

        public bool GetBoolean()
        {
            return ReadData.ReadByte() != 0;
        }

        public int GetInteger()
        {
            return ReadData.ReadInt();
        }

        public double GetDouble()
        {
            return ReadData.ReadDouble();
        }

        public string GetString()
        {
            int remainingData = (int)(ReadData.Length - ReadData.Position);

            var buffer = new byte[remainingData];
            ReadData.ReadExactly(buffer, 0, remainingData);

            return Encoding.UTF8.GetString(buffer);
        }

        public bool[] GetBoolArray()
        {
            var arr = new bool[ReadData.Remaining()];

            int i = 0;
            while (ReadData.Remaining() >= 0)
            {
                var fnd = BitConverter.ToBoolean(Data.ToArray(), ReadData.Remaining());
                ReadData.Position += 2;
                arr[i] = fnd;
                i++;
            }

            return arr;
        }

        public long[] GetIntegerArray()
        {
            var arr = new long[ReadData.Remaining()];

            int i = 0;
            while (ReadData.Remaining() >= 0)
            {
                var fnd = BitConverter.ToInt64(Data.ToArray(), ReadData.Remaining());
                ReadData.Position += 2;
                arr[i] = fnd;
                i++;
            }

            return arr;
        }

        public double[] GetDoubleArray()
        {
            var arr = new double[ReadData.Remaining()];

            int i = 0;
            while (ReadData.Remaining() >= 0)
            {
                var fnd = BitConverter.ToDouble(Data.ToArray(), ReadData.Remaining());
                ReadData.Position += 2;
                arr[i] = fnd;
                i++;
            }

            return arr;
        }

        public string[] GetStringArray()
        {
            var arr = new string[ReadData.Remaining()];

            int i = 0;
            while (ReadData.Remaining() >= 0)
            {
                var fnd = ReadData.ReadInnerString();
                arr[i] = fnd;
                i++;
            }

            return arr;
        }

        public void Dispose()
        {
            ReadData.Dispose();
        }

        public class StartRecordData
        {
            /// <summary>
            /// Entry ID; this will be used for this entry in future records
            /// </summary>
            public readonly int Entry;

            /// <summary>
            /// Entry name
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Type of the stored data for this entry, as a string, e.g. "double".
            /// </summary>
            public readonly string Type;

            /// <summary>
            /// Initial metadata
            /// </summary>
            public readonly string Metadata;

            /// <summary>
            /// Data contained in a start control record.
            /// This can be read by calling GetStartData()
            /// </summary>
            public StartRecordData(int entry, string name, string type, string metadata)
            {
                Entry = entry;
                Name = name;
                Type = type;
                Metadata = metadata;
            }
        }

        public class MetadataRecordData
        {
            /// <summary>
            /// Entry ID.
            /// </summary>
            public readonly int Entry;

            /// <summary>
            /// New metadata for the entry.
            /// </summary>
            public readonly string Metadata;

            /// <summary>
            /// Data contained in a set metadata control record.
            /// This can be read by calling GetSetMetadataData()
            /// </summary>
            /// <param name="entry">Entry ID</param>
            /// <param name="metadata">Metadata for the entry</param>
            public MetadataRecordData(int entry, string metadata)
            {
                Entry = entry;
                Metadata = metadata;
            }
        }


    }

    public enum DataLogRecordType
    {
        Control = 0,
        Start = 1,
        Finish = 2,
    }
}
