using System;
using System.Text;
using System.Collections.Generic;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver;
using LamestWebserver.Compression;
using LamestWebserver.Core;

namespace UnitTests
{
    /// <summary>
    /// Zusammenfassungsbeschreibung für CompressionTest
    /// </summary>
    [TestClass]
    public class CompressionTest
    {
        [TestMethod]
        public void TestCompressDecompress()
        {
            Console.WriteLine("Testing Compression, Decompression:\n" + new string('_', 100));

            for (int i = 0; i < 100; i++)
            {
                string message = Hash.GetComplexHash() + Hash.GetComplexHash()
                                 + Hash.GetComplexHash() + Hash.GetComplexHash();

                while (message.Length > 0)
                {
                    Assert.AreEqual(message, GZipCompression.DecompressString(GZipCompression.CompressString(message, CompressionLevel.NoCompression)));
                    Assert.AreEqual(message, GZipCompression.DecompressString(GZipCompression.CompressString(message, CompressionLevel.Fastest)));
                    Assert.AreEqual(message, GZipCompression.DecompressString(GZipCompression.CompressString(message, CompressionLevel.Optimal)));

                    message = message.Remove(0, 1);
                }

                Console.Write(".");
            }

            Console.WriteLine();
        }
    }
}
