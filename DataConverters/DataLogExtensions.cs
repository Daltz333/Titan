using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Titan.DataConverters
{
    public static class DataLogExtensions
    {
        /// <summary>
        /// Reads an int from the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static int ReadInt(this MemoryStream stream)
        {
            if (stream.Remaining() < 4)
            {
                throw new InvalidDataException("Stream size too small. Expected size 4");
            }


            var buffer = new byte[4];
            stream.ReadExactly(buffer, 0, 4);

            return BitConverter.ToInt32(buffer, 0);
        }

        /// <summary>
        /// Reads a string from the stream, where the first byte is the size of the string
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadInnerString(this MemoryStream stream)
        {
            var size = ReadInt(stream);
            var buffer = new byte[size];

            if (stream.Remaining() < size)
            {
                throw new InvalidDataException($"Stream size too small. Expected {size}");
            }

            _ = stream.Read(buffer, 0, size);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Reads a long from the given stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static long ReadLong(this MemoryStream stream)
        {
            if (stream.Remaining() < 8)
            {
                throw new InvalidDataException("Stream is less than 8 bytes.");
            }

            var buffer = new byte[8];
            stream.ReadExactly(buffer, 0, 8);

            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Reads a double from the given stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static double ReadDouble(this MemoryStream stream)
        {
            if (stream.Remaining() < 8)
            {
                throw new InvalidDataException("Stream is less than 8 bytes.");
            }

            var buffer = new byte[8];
            stream.ReadExactly(buffer, 0, 8);

            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Returns the amount of the remaining bytes in the stream 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static int Remaining(this MemoryStream stream)
        {
            return (int)(stream.Length - stream.Position);
        }
    }
}
