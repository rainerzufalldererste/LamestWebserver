using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamestWebserver.Core
{
    /// <summary>
    /// Provides functionality to generate random hashes and IDs.
    /// </summary>
    public static class Hash
    {
        private static byte[] _lastHash = null;
        private static ICryptoTransform _cryptoTransform = null;

        private static Mutex _hashMutex = new Mutex();
        private static SHA3Managed _sha3;
        private static readonly Mutex Sha3HashMutex = new Mutex();

        /// <summary>
        /// Generates and retrieves a 128 bit AES hash as Hex-String.
        /// </summary>
        /// <returns>The hash.</returns>
        public static string GetHash()
        {
            byte[] bytehash = GetHashBytes();

            string hash = _lastHash.ToHexString();

            return hash;
        }
        
        /// <summary>
        /// Generates and retrieves a 128 bit AES hash.
        /// </summary>
        /// <returns>The hash.</returns>
        public static byte[] GetHashBytes()
        {
            if (_hashMutex == null)
                _hashMutex = new Mutex();

            _hashMutex.WaitOne();
            
            if (_cryptoTransform == null)
            {
                Aes aes = new AesManaged() { Mode = CipherMode.ECB };
                aes.GenerateIV();
                aes.GenerateKey();
                _cryptoTransform = aes.CreateEncryptor();

                if (_lastHash == null)
                    _lastHash = new byte[16];
            }

            _cryptoTransform.TransformBlock(_lastHash, 0, 16, _lastHash, 0);

            _hashMutex.ReleaseMutex();

            return _lastHash;
        }

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input as Hex-String
        /// </summary>
        /// <param name="input">the text to hash</param>
        /// <returns>the hash as base64 string</returns>
        public static string GetComplexHash(string input)
        {
            return Convert.ToBase64String(GetComplexHash(Encoding.Unicode.GetBytes(input)));
        }

        /// <summary>
        /// Generates a SHA3 512 bit Hash of the given input
        /// </summary>
        /// <param name="input">the byte[] to hash</param>
        /// <returns>the hash as byte[]</returns>
        public static byte[] GetComplexHash(byte[] input)
        {
            if (_sha3 == null)
            {
                _sha3 = new SHA3Managed(512);
            }

            Sha3HashMutex.WaitOne();

            byte[] bytes = _sha3.ComputeHash(input);

            Sha3HashMutex.ReleaseMutex();

            return bytes;
        }

        private static string _lastSha3Hash = GetHash() + GetHash() + GetHash() + GetHash();

        /// <summary>
        /// Generates a SHA3 512bit hash of random data.
        /// </summary>
        /// <returns>the hash as base64 string</returns>
        public static string GetComplexHash()
        {
            return _lastSha3Hash = GetComplexHash(_lastSha3Hash + GetHash());
        }

        /// <summary>
        /// Generates a SHA3 512bit hash of random data.
        /// </summary>
        /// <returns>the hash as byte[]</returns>
        public static byte[] GetComplexHashBytes()
        {
            byte[] ret = GetComplexHash(Encoding.Unicode.GetBytes(_lastSha3Hash + GetHash()));

            _lastSha3Hash = Convert.ToBase64String(ret);

            return ret;
        }
    }
}
