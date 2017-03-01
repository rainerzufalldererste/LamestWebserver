using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Security
{
    /// <summary>
    /// Provides basic functionality for encryption and decryption.
    /// </summary>
    public static class Encryption
    {
        /// <summary>
        /// Encrypts a given string with AES128 CounterMode.
        /// </summary>
        /// <param name="message">the message to encrypt</param>
        /// <param name="key">the key (32 bytes recommended)</param>
        /// <param name="iv">the iv (16 bytes)</param>
        /// <returns>the encrypted message as base64 encoded string</returns>
        public static string Encrypt(string message, byte[] key, byte[] iv)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(message), key, iv));
        }

        /// <summary>
        /// Decrypts a given Base64 encoded string using AES128 in CounterMode
        /// </summary>
        /// <param name="message">the encrypted message</param>
        /// <param name="key">the key (32 bytes recommended)</param>
        /// <param name="iv">the iv (16 bytes)</param>
        /// <returns>the decrypted result as UTF8-string</returns>
        public static string Decrypt(string message, byte[] key, byte[] iv)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(message), key, iv));
        }

        /// <summary>
        /// Encrypts a given byte[] with AES128 CounterMode.
        /// </summary>
        /// <param name="message">the message to encrypt</param>
        /// <param name="key">the key (32 bytes recommended)</param>
        /// <param name="iv">the iv (16 bytes)</param>
        /// <returns>the encrypted message as byte[]</returns>
        public static byte[] Encrypt(byte[] message, byte[] key, byte[] iv)
        {
            if (iv.Length != 16) throw new ArgumentException(nameof(iv));
            if (key.Length % 16 != 0) throw new ArgumentException(nameof(key));

            ICryptoTransform transform = new AesManaged() { Mode = CipherMode.CBC }.CreateEncryptor(key, iv);

            return transform.TransformFinalBlock(message, 0, message.Length);
        }

        /// <summary>
        /// Decrypts a given byte[] using AES128 in CounterMode
        /// </summary>
        /// <param name="message">the encrypted message</param>
        /// <param name="key">the key (32 bytes recommended)</param>
        /// <param name="iv">the iv (16 bytes)</param>
        /// <returns>returns the decrypted result as byte[]</returns>
        public static byte[] Decrypt(byte[] message, byte[] key, byte[] iv)
        {
            if (iv.Length != 16) throw new ArgumentException(nameof(iv));
            if (key.Length % 16 != 0) throw new ArgumentException(nameof(key));

            ICryptoTransform transform = new AesManaged() { Mode = CipherMode.CBC }.CreateDecryptor(key, iv);

            return transform.TransformFinalBlock(message, 0, message.Length);
        }

        /// <summary>
        /// Generates a secure 32 byte key.
        /// </summary>
        /// <returns>the key</returns>
        public static byte[] GetKey()
        {
            var aes = new AesManaged();
            aes.GenerateKey();

            return aes.Key;
        }

        /// <summary>
        /// Generates a secure 16 byte initialization vector.
        /// </summary>
        /// <returns>the IV</returns>
        public static byte[] GetIV()
        {
            var aes = new AesManaged();
            aes.GenerateIV();

            return aes.IV;
        }
    }
}
