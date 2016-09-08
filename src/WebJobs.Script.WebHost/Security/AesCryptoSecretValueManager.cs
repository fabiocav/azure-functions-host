// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class AesCryptoSecretValueManager : ISecretValueManager
    {
        internal const string DefaultEncryptionKeyId = "AzureWebJobsSecretEncryptionKeyId";
        private static ConcurrentDictionary<string, byte[]> _keys = new ConcurrentDictionary<string, byte[]>();
        
        public string ReadKeyValue(Key key)
        {
            Tuple<byte[], string> encryptionKeyData = GetEncryptionKey(key);

            return DecryptValue(key.Value, encryptionKeyData.Item1);
        }

        public Key WriteKeyValue(Key key)
        {
            Tuple<byte[], string> encryptionKeyData = GetEncryptionKey(key);

            string encryptedValue = EncryptValue(key.Value, encryptionKeyData.Item1);

            return new Key
            {
                Name = key.Name,
                EncryptionKeyId = encryptionKeyData.Item2,
                Value = encryptedValue,
                IsEncrypted = true
            };
        }

        private static Tuple<byte[], string> GetEncryptionKey(Key key)
        {
            string keyName = string.IsNullOrEmpty(key.EncryptionKeyId) ? 
                Environment.GetEnvironmentVariable(DefaultEncryptionKeyId) : key.EncryptionKeyId;

            if (keyName == null)
            {
                throw new InvalidOperationException("Cryptographic error. Missing key configuration.");
            }

            byte[] encryptionKey;

            if (!_keys.TryGetValue(keyName, out encryptionKey))
            {
                string keyString = Environment.GetEnvironmentVariable(keyName);
                if (keyString == null)
                {
                    throw new InvalidOperationException("Cryptographic error. Key ID not found.");
                }

                encryptionKey = _keys.GetOrAdd(keyName, k => Convert.FromBase64String(keyString));
            }

            return new Tuple<byte[], string>(encryptionKey, keyName);
        }

        private static string EncryptValue(string value, byte[] encryptionKey)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = encryptionKey;
                aes.GenerateIV();

                ICryptoTransform encryptor = aes.CreateEncryptor();

                using (var resultStream = new MemoryStream())
                {
                    // Write the IV to the stream (IV will prepend the encrypted data)
                    resultStream.Write(aes.IV, 0, aes.IV.Length);

                    using (var cryptoStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (var writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(value);
                        }

                        return Convert.ToBase64String(resultStream.ToArray());
                    }
                }
            }
        }

        private static string DecryptValue(string value, byte[] encryptionKey)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.Key = encryptionKey;

                byte[] encryptedData = Convert.FromBase64String(value);

                int blockSizeInBytes = aes.BlockSize / 8;
                byte[] iv = encryptedData.Take(blockSizeInBytes).ToArray();

                ICryptoTransform decryptor = aes.CreateDecryptor(encryptionKey, iv);

                using (var stream = new MemoryStream(encryptedData, blockSizeInBytes, encryptedData.Length - blockSizeInBytes))
                {
                    using (var cryptoStream = new CryptoStream(stream, decryptor, CryptoStreamMode.Read))
                    {
                        using (var writer = new StreamReader(cryptoStream))
                        {
                            return writer.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
