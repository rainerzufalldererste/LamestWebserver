using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Compression
{
    public static class GZipCompression
    {
        public static byte[] CompressString(string data, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            return Compress(Encoding.UTF8.GetBytes(data), compressionLevel);
        }

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
        public static string DecompressString(byte[] data)
        {
            return Encoding.UTF8.GetString(Decompress(data));
        }

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
