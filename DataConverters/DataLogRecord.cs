using System;
using System.IO;
using System.Text;

namespace Titan.DataConverters
{
    public class DataLogRecord
    {
        private const int kControlStart = 0;
        private const int kControlFinish = 1;
        private const int kControlSetMetadata = 2;

        public int Entry { get; private set; } = 0;
        public long Timestamp { get; private set; } = 0;
        public int Size { get => Buffer.Length; }

        private ReadOnlyMemory<byte> Buffer;

        private int BytePosition = 0;

        public DataLogRecord(int entry, long timestamp, ReadOnlyMemory<byte> data)
        {
            Entry = entry;
            Timestamp = timestamp;
            Buffer = data;
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
            return Entry == 0 && Buffer.Length >= 16 && Buffer.Span[0] == kControlStart;
        }

        /// <summary>
        /// Returns true if the record is a finish control record. Use GetFinishEntry()
        /// to decode the contents.
        /// </summary>
        /// <returns>True if finish control record, false otherwise</returns>
        public bool IsFinish()
        {
            return Entry == 0 && Buffer.Length >= 5 && Buffer.Span[0] == kControlFinish;
        }

        /// <summary>
        /// Returns true if the record is a set metadata control record.
        /// Use GetSetMetadataData() to decode the contents.
        /// </summary>
        /// <returns>True if set metadata control record, false otherwise</returns>
        public bool IsSetMetadata()
        {
            return Entry == 0 && Buffer.Length >= 9 && Buffer.Span[0] == kControlSetMetadata;
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

            int entry = BitConverter.ToInt32(Buffer.Slice(1, 4).ToArray());

            int nameSize = BitConverter.ToInt32(Buffer.Slice(1 + 4, 4).ToArray());
            string name = Encoding.UTF8.GetString(Buffer.Slice(1 + 4 + 4, nameSize).ToArray());

            int typeSize = BitConverter.ToInt32(Buffer.Slice(1 + 4 + 4 + nameSize, 4).ToArray());
            string type = Encoding.UTF8.GetString(Buffer.Slice(1 + 4 + 4 + nameSize + 4, typeSize).ToArray());

            int metadataSize = BitConverter.ToInt32(Buffer.Slice(1 + 4 + 4 + nameSize + 4 + typeSize, 4).ToArray());
            string metadata = Encoding.UTF8.GetString(Buffer.Slice(1 + 4 + 4 + nameSize + 4 + typeSize + 4, metadataSize).ToArray());

            BytePosition = 1 + 4 + 4 + nameSize + 4 + typeSize + 4 + metadataSize;

            return new StartRecordData(entry, name, type, metadata);
        }

        public MetadataRecordData GetSetMetadataData()
        {
            if (!IsSetMetadata())
            {
                throw new InvalidOperationException($"Called {nameof(GetSetMetadataData)} on an incorrect record type.");
            }

            using var stream = new MemoryStream(Buffer.ToArray());

            int entry = BitConverter.ToInt32(Buffer.Slice(1, 4).ToArray());

            int metadataSize = BitConverter.ToInt32(Buffer.Slice(1 + 4, 4).ToArray());
            string metadata = Encoding.UTF8.GetString(Buffer.Slice(1 + 4 + 4, metadataSize).ToArray());

            BytePosition = 1 + 4 + 4 + metadataSize;

            return new MetadataRecordData(entry, metadata);
        }

        public bool GetBoolean()
        {
            var res = Buffer.Span[BytePosition] != 0;
            BytePosition += 1;
            return res;
        }

        public long GetInteger()
        {
            var res = BitConverter.ToInt64(Buffer.Slice(BytePosition, 8).ToArray());
            BytePosition += 8;
            return res;
        }

        public double GetDouble()
        {
            var res = BitConverter.ToInt32(Buffer.Slice(BytePosition, 8).ToArray());
            BytePosition += 8;
            return res;
        }

        public string GetString()
        {
            var res = Encoding.UTF8.GetString(Buffer.Slice(BytePosition).ToArray());
            BytePosition = Buffer.Length;
            return res;
        }

        public bool[] GetBoolArray()
        {
            var arr = new bool[Buffer.Length - BytePosition];

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = Buffer.Slice(BytePosition).Span[i] != 0;
                i++;

                BytePosition += 1;
            }

            return arr;
        }

        public long[] GetIntegerArray()
        {
            var arr = new long[Buffer.Length - BytePosition];

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = BitConverter.ToInt64(Buffer.Slice(BytePosition, 8).ToArray());
                i++;

                BytePosition += 8;
            }

            return arr;
        }

        public double[] GetDoubleArray()
        {
            var arr = new double[Buffer.Length - BytePosition];

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = BitConverter.ToInt64(Buffer.Slice(BytePosition, 8).ToArray());
                i++;

                BytePosition += 8;
            }

            return arr;
        }

        public string[] GetStringArray()
        {
            var arr = new string[Buffer.Length - BytePosition];

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                var strSize = BitConverter.ToInt32(Buffer.Slice(BytePosition, 4).ToArray());
                arr[i] = Encoding.UTF8.GetString(Buffer.Slice(BytePosition + 4, strSize).ToArray());
                i++;

                BytePosition += BytePosition + 4 + strSize;
            }

            return arr;
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
