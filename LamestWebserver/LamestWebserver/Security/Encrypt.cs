using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LamestWebserver.Security
{
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

            byte[] realMessage = new byte[message.Length + 64];

            Array.Copy(SessionContainer.GetComplexHash(message), realMessage, 64);
            Array.Copy(message, 0, realMessage, 64, message.Length);

            ICryptoTransform transform = new Aes128CounterMode(iv).CreateEncryptor(key, iv);

            byte[] resultingMessage = transform.TransformFinalBlock(realMessage, 0, realMessage.Length);

            return resultingMessage;
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

    /// <summary>
    /// Source: https://gist.github.com/hanswolff/8809275
    /// </summary>
    public class Aes128CounterMode : SymmetricAlgorithm
    {
        private readonly byte[] _counter;
        private readonly AesManaged _aes;

        /// <summary>
        /// initializes a new Aes128ManagedModeAlgorithm.
        /// </summary>
        /// <param name="counter">the counter</param>
        public Aes128CounterMode(byte[] counter)
        {
            if (counter == null) throw new ArgumentNullException(nameof(counter));
            if (counter.Length != 16)
                throw new ArgumentException($"Counter size must be same as block size (actual: {counter.Length}, expected: {16})");

            _aes = new AesManaged
            {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None
            };

            _counter = counter;
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] ignoredParameter)
        {
            return new CounterModeCryptoTransform(_aes, rgbKey, _counter);
        }

        /// <inheritdoc />
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] ignoredParameter)
        {
            return new CounterModeCryptoTransform(_aes, rgbKey, _counter);
        }

        /// <inheritdoc />
        public override void GenerateKey()
        {
            _aes.GenerateKey();
        }

        /// <inheritdoc />
        public override void GenerateIV()
        {
            // IV not needed in Counter Mode
        }
    }

    /// <summary>
    /// Source: https://gist.github.com/hanswolff/8809275
    /// </summary>
    public class CounterModeCryptoTransform : ICryptoTransform
    {
        private readonly byte[] _counter;
        private readonly ICryptoTransform _counterEncryptor;
        private readonly Queue<byte> _xorMask = new Queue<byte>();
        private readonly SymmetricAlgorithm _symmetricAlgorithm;

        /// <summary>
        /// Used in Aes128CounterMode.
        /// </summary>
        public CounterModeCryptoTransform(SymmetricAlgorithm symmetricAlgorithm, byte[] key, byte[] counter)
        {
            if (symmetricAlgorithm == null) throw new ArgumentNullException(nameof(symmetricAlgorithm));
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (counter == null) throw new ArgumentNullException(nameof(counter));
            if (counter.Length != symmetricAlgorithm.BlockSize/8)
                throw new ArgumentException($"Counter size must be same as block size (actual: {counter.Length}, expected: {symmetricAlgorithm.BlockSize/8})");

            _symmetricAlgorithm = symmetricAlgorithm;
            _counter = counter;

            var zeroIv = new byte[_symmetricAlgorithm.BlockSize/8];
            _counterEncryptor = symmetricAlgorithm.CreateEncryptor(key, zeroIv);
        }

        /// <inheritdoc />
        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            var output = new byte[inputCount];
            TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
            return output;
        }

        /// <inheritdoc />
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            for (var i = 0; i < inputCount; i++)
            {
                if (NeedMoreXorMaskBytes()) EncryptCounterThenIncrement();

                var mask = _xorMask.Dequeue();
                outputBuffer[outputOffset + i] = (byte)(inputBuffer[inputOffset + i] ^ mask);
            }

            return inputCount;
        }

        private bool NeedMoreXorMaskBytes()
        {
            return _xorMask.Count == 0;
        }

        private void EncryptCounterThenIncrement()
        {
            var counterModeBlock = new byte[_symmetricAlgorithm.BlockSize / 8];

            _counterEncryptor.TransformBlock(_counter, 0, _counter.Length, counterModeBlock, 0);
            IncrementCounter();

            foreach (var b in counterModeBlock)
            {
                _xorMask.Enqueue(b);
            }
        }

        private void IncrementCounter()
        {
            for (var i = _counter.Length - 1; i >= 0; i--)
            {
                if (++_counter[i] != 0)
                    break;
            }
        }

        /// <inheritdoc />
        public int InputBlockSize => _symmetricAlgorithm.BlockSize / 8;

        /// <inheritdoc />
        public int OutputBlockSize => _symmetricAlgorithm.BlockSize / 8;

        /// <inheritdoc />
        public bool CanTransformMultipleBlocks => true;

        /// <inheritdoc />
        public bool CanReuseTransform => false;

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
