// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            var converter = new AesCryptoKeyValueConverter(FileAccess.ReadWrite);

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(EnvironmentSettingNames.AzureWebLocalEncryptionKey, "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value", EncryptionKeyId = keyId };

                // Encrypt the key
                var resultKey = converter.WriteValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = converter.ReadValue(resultKey);

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
            var converter = new AesCryptoKeyValueConverter(FileAccess.ReadWrite);

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(EnvironmentSettingNames.AzureWebJobsDefaultEncryptionKeyId, keyId);
                Environment.SetEnvironmentVariable(keyId, "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value" };

                // Encrypt the key
                Key resultKey = converter.WriteValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = converter.ReadValue(resultKey);

                Assert.Equal(testInputKey.Value, decryptedSecret);
                Assert.Equal(keyId, resultKey.EncryptionKeyId);
            }
            finally
            {
                Environment.SetEnvironmentVariable(keyId, null);
                Environment.SetEnvironmentVariable(EnvironmentSettingNames.AzureWebJobsDefaultEncryptionKeyId, null);
            }
        }

        [Fact]
        public void WriteValue_WithReadAccess_ThrowsExpectedException()
        {
            var converter = new AesCryptoKeyValueConverter(FileAccess.Read);
            Assert.Throws<InvalidOperationException>(() => converter.WriteValue(new Key()));
        }

        [Fact]
        public void ReadValue_WithWriteAccess_ThrowsExpectedException()
        {
            var converter = new AesCryptoKeyValueConverter(FileAccess.Write);
            Assert.Throws<InvalidOperationException>(() => converter.ReadValue(new Key()));
        }

        [Fact]
        public void WriteKeyValue_WithMissingKeyConfiguration_ThrowsExpectedException()
        {
            TestKeyConfigurationException((m, k) => m.WriteValue(k), FileAccess.Write);
        }

        [Fact]
        public void WriteKeyValue_WithInvalidKeyId_ThrowsExpectedException()
        {
            TestKeyConfigurationException((m, k) => m.WriteValue(k), FileAccess.Write, "INVALID");
        }

        [Fact]
        public void ReadKeyValue_WithInvalidKeyId_ThrowsExpectedException()
        {
            TestKeyConfigurationException((m, k) => m.ReadValue(k), FileAccess.Read, "INVALID");
        }

        private void TestKeyConfigurationException(Action<AesCryptoKeyValueConverter, Key> keyOperation, FileAccess access, string keyId = null)
        {
            var converter = new AesCryptoKeyValueConverter(access);

            // Create our test input key
            var testInputKey = new Key { Name = "Test", Value = "Test secret value", EncryptionKeyId = keyId };

            CryptographicException exception = Assert.Throws<CryptographicException>(() => keyOperation(converter, testInputKey));

            Assert.Equal("Missing key configuration.", exception.Message);
        }
    }
}
