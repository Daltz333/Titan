using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Titan.DataConverters
{
    public class DataLogReader(string filename)
    {
        private readonly ReadOnlyMemory<byte> Buffer = File.ReadAllBytes(filename);

        public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
        public event ProgressChangedEventHandler? ProgressChanged;


        /// <summary>
        /// Returns true if the data log is valid (e.g. has a valid header)
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            var bufferSpan = Buffer.Span;

            var headerMatch = bufferSpan.Length >= 12 && bufferSpan[..6].SequenceEqual("WPILOG"u8);

            return headerMatch && (BitConverter.ToInt16(bufferSpan.Slice(6, 2)) >= 0x0100);
        }

        public Version GetVersion()
        {
            var bufferSpan = Buffer.Span;

            if (bufferSpan.Length < 12)
            {
                return new();
            }

            var versionBytes = bufferSpan.Slice(6, 2);
            return new Version(versionBytes[1], versionBytes[0]);
        }

        /// <summary>
        /// Gets the extra header data
        /// </summary>
        /// <returns>Extra header data</returns>
        public string GetExtraHeader()
        {
            var bufferSpan = Buffer.Span;
            int size = BitConverter.ToInt32(bufferSpan.Slice(8, 4));
            return Encoding.UTF8.GetString(bufferSpan.Slice(12, size));
        }

        private DataLogRecord GetRecord(int position)
        {
            var bufferSpan = Buffer.Span;

            int lenBytes = bufferSpan[position] & 0xff;
            int entryLen = (lenBytes & 0x3) + 1;
            int sizeLen = ((lenBytes >> 2) & 0x3) + 1;
            int timestampLen = ((lenBytes >> 4) & 0x7) + 1;

            int headerLen = 1 + entryLen + sizeLen + timestampLen;
            int entry = (int)ReadVarInt(position + 1, entryLen);
            int size = (int)ReadVarInt(position + 1 + entryLen, sizeLen);
            long timestamp = ReadVarInt(position + 1 + entryLen + sizeLen, timestampLen);

            // build a slice of the data contents
            return new DataLogRecord(entry, timestamp, Buffer.Slice(position + headerLen, size));
        }

        private long ReadVarInt(int pos, int len)
        {
            var bufferSpan = Buffer.Span;

            long val = 0;
            for (int i = 0; i < len; i++)
            {
                val |= ((long)bufferSpan[pos + i] & 0xff) << (i * 8);
            }

            return val;
        }

        private int GetNextRecord(int pos)
        {
            var bufferSpan = Buffer.Span;

            int lenbyte = bufferSpan[pos] & 0xff;
            int entryLen = (lenbyte & 0x3) + 1;
            int sizeLen = ((lenbyte >> 2) & 0x3) + 1;
            int timestampLen = ((lenbyte >> 4) & 0x7) + 1;
            int headerLen = 1 + entryLen + sizeLen + timestampLen;

            int size = 0;
            for (int i = 0; i < sizeLen; i++)
            {
                size |= (bufferSpan[(pos + 1 + entryLen + i)] & 0xff) << (i * 8);
            }
            return pos + headerLen + size;
        }

        public List<DataLogRecord> GetRecords()
        {
            var bufferSpan = Buffer.Span;
            int pos = 12 + BitConverter.ToInt32(bufferSpan.Slice(8, 4));
            var records = new List<DataLogRecord>();

            while (true)
            {
                DataLogRecord record;

                try
                {
                    pos = GetNextRecord(pos);
                    record = GetRecord(pos);
                }
                catch (IndexOutOfRangeException)
                {
                    break;
                }

                if (bufferSpan.Length > 0)
                {
                    ProgressChanged?.Invoke(this, new(Math.Round((double)pos / bufferSpan.Length, 2)));
                }

                records.Add(record);
            }

            return records;
        }

        public record struct ProgressChangedEventArgs(double Progress);
    }
}
