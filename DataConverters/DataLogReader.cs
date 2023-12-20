using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Titan.DataConverters
{
    public class DataLogReader
    {
        private readonly ReadOnlyMemory<byte> Buffer;

        public delegate void ProgressChangedEventHandler(object sender, ProgressChangedEventArgs e);
        public event ProgressChangedEventHandler? ProgressChanged;

        private readonly long TotalSizeBytes;

        public DataLogReader(string filename)
        {
            TotalSizeBytes = new FileInfo(filename).Length;

            Buffer = File.ReadAllBytes(filename);
        }

        /// <summary>
        /// Returns true if the data log is valid (e.g. has a valid header)
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            var headerMatch = Buffer.Length >= 12
                && Buffer.Span[0] == (byte)'W'
                && Buffer.Span[1] == (byte)'P'
                && Buffer.Span[2] == (byte)'I'
                && Buffer.Span[3] == (byte)'L'
                && Buffer.Span[4] == (byte)'O'
                && Buffer.Span[5] == (byte)'G';

            var buffer = new byte[4];
            using MemoryStream stream = new MemoryStream(Buffer.Slice(6, 4).ToArray());
            _ = stream.Read(buffer, 0, 4);

            return headerMatch && (BitConverter.ToInt16(buffer, 0) >= 0x0100);
        }

        public Version GetVersion()
        {
            if (Buffer.Length < 12)
            {
                return new();
            }

            var versionBytes = Buffer.Slice(6, 2).Span;
            var versionMajor = new byte[8];
            versionMajor[0] = versionBytes[1];

            var versionMinor = new byte[8];
            versionMinor[0] = versionBytes[0];

            return new Version(BitConverter.ToInt32(versionMajor), BitConverter.ToInt32(versionMinor));
        }

        /// <summary>
        /// Gets the extra header data
        /// </summary>
        /// <returns>Extra header data</returns>
        public string GetExtraHeader()
        {
            int size = BitConverter.ToInt32(Buffer.Slice(8, 4).ToArray());
            return Encoding.UTF8.GetString(Buffer.Slice(12, size).ToArray());
        }

        private DataLogRecord GetRecord(int position)
        {
            int lenBytes = Buffer.Span[position] & 0xff;
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
            long val = 0;
            for (int i = 0; i < len; i++)
            {
                val |= ((long)(Buffer.Span[pos + i]) & 0xff) << (i * 8);
            }

            return val;
        }

        private int GetNextRecord(int pos)
        {
            int lenbyte = Buffer.Span[pos] & 0xff;
            int entryLen = (lenbyte & 0x3) + 1;
            int sizeLen = ((lenbyte >> 2) & 0x3) + 1;
            int timestampLen = ((lenbyte >> 4) & 0x7) + 1;
            int headerLen = 1 + entryLen + sizeLen + timestampLen;

            int size = 0;
            for (int i = 0; i < sizeLen; i++)
            {
                size |= (Buffer.Span[(pos + 1 + entryLen + i)] & 0xff) << (i * 8);
            }
            return pos + headerLen + size;
        }

        public List<DataLogRecord> GetRecords()
        {
            int pos = 12 + BitConverter.ToInt32(Buffer.Slice(8, 4).ToArray());
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

                if (TotalSizeBytes > 0)
                {
                    ProgressChanged?.Invoke(this, new(Math.Round(((double)pos / TotalSizeBytes), 2)));
                }

                records.Add(record);
            }

            return records;
        }

        public class ProgressChangedEventArgs(double progress)
        {
            public double Progress { get; private set; } = progress;
        }
    }
}