using System;
using System.Text;
using System.Collections.Generic;
using LamestWebserver;
using LamestWebserver.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LamestWebserver.Core;

namespace UnitTests
{
    /// <summary>
    /// Zusammenfassungsbeschreibung für EncryptDecryptTests
    /// </summary>
    [TestClass]
    public class EncryptDecryptTests
    {
        [TestMethod]
        public void TestEncryptDecrypt()
        {
            Console.WriteLine("Testing Encryption, Decryption:\n" + new string('_', 100));

            for (int i = 0; i < 100; i++)
            {
                byte[] key = Encryption.GetKey();
                byte[] iv = Encryption.GetIV();

                string message = Hash.GetComplexHash() + Hash.GetComplexHash()
                                 + Hash.GetComplexHash() + Hash.GetComplexHash();

                while (message.Length > 0)
                {
                    var enc = Encryption.Encrypt(message, key, iv);
                    var dec = Encryption.Decrypt(enc, key, iv);

                    Assert.AreEqual(message, dec);

                    message = message.Remove(0, 1);
                }

                Console.Write(".");
            }

            Console.WriteLine();
        }
    }
}
