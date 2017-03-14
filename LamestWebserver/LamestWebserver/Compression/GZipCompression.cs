using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Compression
{
    /// <summary>
    /// Includes Methods for GZip Compressing and Decompressing.
    /// </summary>
    public static class GZipCompression
    {
        /// <summary>
        /// Compresses a string by UTF8-Encoding it and Compressing the bytes
        /// </summary>
        /// <param name="data">the string to compress</param>
        /// <param name="compressionLevel">the level of compression</param>
        /// <returns>the compressed data as byte[]</returns>
        public static byte[] CompressString(string data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return Compress(Encoding.UTF8.GetBytes(data), compressionLevel);
        }

        /// <summary>
        /// Compresses a byte[]
        /// </summary>
        /// <param name="data">the byte[] to compress</param>
        /// <param name="compressionLevel">the level of compression</param>
        /// <returns>the compressed data as byte[]</returns>
        public static byte[] Compress(byte[] data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            using (var memStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memStream, compressionLevel))
                {
                    gzipStream.Write(data, 0, data.Length);
                }

                return memStream.ToArray();
            }
        }

        /// <summary>
        /// Decompressed a byte array and encodes the results in UTF-8
        /// </summary>
        /// <param name="data">the bytes to decompress</param>
        /// <returns>the resulting string</returns>
        public static string DecompressString(byte[] data)
        {
            return Encoding.UTF8.GetString(Decompress(data));
        }

        /// <summary>
        /// Decompressed a byte array
        /// </summary>
        /// <param name="data">the bytes to decompress</param>
        /// <returns>the resulting byte[]</returns>
        public static byte[] Decompress(byte[] data)
        {
            using (var memStream = new MemoryStream(data))
            {
                using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
                {
                    using (var innerMemStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(innerMemStream);
                        return innerMemStream.ToArray();
                    }
                }
            }
        }
    }
}
