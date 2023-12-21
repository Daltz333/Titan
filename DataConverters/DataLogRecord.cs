using System;
using System.IO;
using System.Text;

namespace Titan.DataConverters
{
    public class DataLogRecord(int entry, long timestamp, ReadOnlyMemory<byte> data)

    {
        private const int kControlStart = 0;
        private const int kControlFinish = 1;
        private const int kControlSetMetadata = 2;

        public int Entry { get; private set; } = entry;
        public long Timestamp { get; private set; } = timestamp;
        public int Size { get => Buffer.Length; }

        private readonly ReadOnlyMemory<byte> Buffer = data;

        private int BytePosition = 0;

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

            var bufferSpan = Buffer.Span;

            int entry = BitConverter.ToInt32(bufferSpan.Slice(1, 4));

            int nameSize = BitConverter.ToInt32(bufferSpan.Slice(1 + 4, 4));
            string name = Encoding.UTF8.GetString(bufferSpan.Slice(1 + 4 + 4, nameSize));

            int typeSize = BitConverter.ToInt32(bufferSpan.Slice(1 + 4 + 4 + nameSize, 4));
            string type = Encoding.UTF8.GetString(bufferSpan.Slice(1 + 4 + 4 + nameSize + 4, typeSize));

            int metadataSize = BitConverter.ToInt32(bufferSpan.Slice(1 + 4 + 4 + nameSize + 4 + typeSize, 4));
            string metadata = Encoding.UTF8.GetString(bufferSpan.Slice(1 + 4 + 4 + nameSize + 4 + typeSize + 4, metadataSize));

            BytePosition = 1 + 4 + 4 + nameSize + 4 + typeSize + 4 + metadataSize;

            return new StartRecordData(entry, name, type, metadata);
        }

        public MetadataRecordData GetSetMetadataData()
        {
            if (!IsSetMetadata())
            {
                throw new InvalidOperationException($"Called {nameof(GetSetMetadataData)} on an incorrect record type.");
            }

            var bufferSpan = Buffer.Span;

            int entry = BitConverter.ToInt32(bufferSpan.Slice(1, 4));

            int metadataSize = BitConverter.ToInt32(bufferSpan.Slice(1 + 4, 4));
            string metadata = Encoding.UTF8.GetString(bufferSpan.Slice(1 + 4 + 4, metadataSize));

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
            var res = BitConverter.ToInt64(Buffer.Span.Slice(BytePosition, 8));
            BytePosition += 8;
            return res;
        }

        public double GetDouble()
        {
            var res = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(Buffer.Span.Slice(BytePosition, 8)));
            BytePosition += 8;
            return res;
        }

        public string GetString()
        {
            var res = Encoding.UTF8.GetString(Buffer.Span[BytePosition..]);
            BytePosition = Buffer.Length;
            return res;
        }

        public bool[] GetBoolArray()
        {
            var arr = new bool[Buffer.Length - BytePosition];
            var bufferSpan = Buffer.Span;

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = bufferSpan[BytePosition] != 0;
                i++;

                BytePosition += 1;
            }

            return arr;
        }

        public long[] GetIntegerArray()
        {
            var arr = new long[Buffer.Length - BytePosition];
            var bufferSpan = Buffer.Span;

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = BitConverter.ToInt64(bufferSpan.Slice(BytePosition, 8));
                i++;

                BytePosition += 8;
            }

            return arr;
        }

        public double[] GetDoubleArray()
        {
            var arr = new double[Buffer.Length - BytePosition];
            var bufferSpan = Buffer.Span;

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                arr[i] = BitConverter.Int64BitsToDouble(BitConverter.ToInt64(bufferSpan.Slice(BytePosition, 8)));
                i++;

                BytePosition += 8;
            }

            return arr;
        }

        public string[] GetStringArray()
        {
            var arr = new string[Buffer.Length - BytePosition];
            var bufferSpan = Buffer.Span;

            int i = 0;
            while (Buffer.Length - BytePosition >= 0)
            {
                var strSize = BitConverter.ToInt32(bufferSpan.Slice(BytePosition, 4));
                arr[i] = Encoding.UTF8.GetString(bufferSpan.Slice(BytePosition + 4, strSize));
                i++;

                BytePosition += 4 + strSize;
            }

            return arr;
        }

        /// <summary>
        /// Data contained in a start control record.
        /// This can be read by calling GetStartData()
        /// </summary>
        public record StartRecordData(int Entry, string Name, string Type, string Metadata);

        /// <summary>
        /// Data contained in a set metadata control record.
        /// This can be read by calling GetSetMetadataData()
        /// </summary>
        /// <param name="entry">Entry ID</param>
        /// <param name="metadata">Metadata for the entry</param>
        public record MetadataRecordData(int Entry, string Metadata);
    }

    public enum DataLogRecordType
    {
        Control = 0,
        Start = 1,
        Finish = 2,
    }
}
