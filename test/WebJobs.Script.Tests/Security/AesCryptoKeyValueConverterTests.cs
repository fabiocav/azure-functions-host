// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.WebHost;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests.Security
{
    public class AesCryptoKeyValueConverterTests
    {
        [Fact]
        public void ReadKeyValue_CanRead_WrittenKey()
        {
            var converter = new AesCryptoKeyValueConverter();

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(keyId, "3H0vEYkSMENnRFrsSDEpWrzig+Hu05RnyL3hNEkzv6Q=");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value", EncryptionKeyId = keyId };

                // Encrypt the key
                var resultKey = converter.WriteKeyValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = converter.ReadKeyValue(resultKey);

                Assert.Equal(testInputKey.Value, decryptedSecret);
            }
            finally
            {
                Environment.SetEnvironmentVariable(keyId, null);
            }
        }

        [Fact]
        public void WriteKeyValue_WithUnspecifiedKeyId_UsesDefaultKey()
        {
            var converter = new AesCryptoKeyValueConverter();

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(AesCryptoKeyValueConverter.DefaultEncryptionKeyId, keyId);
                Environment.SetEnvironmentVariable(keyId, "3H0vEYkSMENnRFrsSDEpWrzig+Hu05RnyL3hNEkzv6Q=");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value" };

                // Encrypt the key
                var resultKey = converter.WriteKeyValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = converter.ReadKeyValue(resultKey);

                Assert.Equal(testInputKey.Value, decryptedSecret);
                Assert.Equal(keyId, resultKey.EncryptionKeyId);
            }
            finally
            {
                Environment.SetEnvironmentVariable(keyId, null);
                Environment.SetEnvironmentVariable(AesCryptoKeyValueConverter.DefaultEncryptionKeyId, null);
            }
        }

        [Fact]
        public void WriteKeyValue_WithMissingKeyConfiguration_ThrowsExpectedException()
        {
            TestKeyConfigurationException((m, k) => m.WriteKeyValue(k));
        }

        [Fact]
        public void WriteKeyValue_WithInvalidKeyId_ThrowsExpectedException()
        {
            TestKeyConfigurationException((m, k) => m.WriteKeyValue(k), "INVALID");
        }

        [Fact]
        public void ReadKeyValue_WithInvalidKeyId_ThrowsExpectedException()
        {
           TestKeyConfigurationException((m, k) => m.ReadKeyValue(k), "INVALID");
        }

        private void TestKeyConfigurationException(Action<AesCryptoKeyValueConverter, Key> keyOperation, string keyId = null)
        {
            var converter = new AesCryptoKeyValueConverter();

            // Create our test input key
            var testInputKey = new Key { Name = "Test", Value = "Test secret value", EncryptionKeyId = keyId };

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => keyOperation(converter, testInputKey));

            string message = null;
            if (!string.IsNullOrEmpty(keyId))
            {
                message = "Cryptographic error. Key ID not found.";
            }
            else
            {
                message = "Cryptographic error. Missing key configuration.";
            }
            
            Assert.Equal(message, exception.Message);
        }
    }
}
