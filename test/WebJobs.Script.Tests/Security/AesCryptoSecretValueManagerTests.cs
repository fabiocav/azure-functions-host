// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.WebHost;
using Microsoft.Azure.WebJobs.Script.WebHost.Security;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests.Security
{
    public class AesCryptoSecretValueManagerTests
    {
        [Fact]
        public void ReadKeyValue_CanRead_WrittenKey()
        {
            var valueManager = new AesCryptoSecretValueManager();

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(keyId, "3H0vEYkSMENnRFrsSDEpWrzig+Hu05RnyL3hNEkzv6Q=");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value", KeyId = keyId };

                // Encrypt the key
                var resultKey = valueManager.WriteKeyValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = valueManager.ReadKeyValue(resultKey);

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
            var valueManager = new AesCryptoSecretValueManager();

            string keyId = Guid.NewGuid().ToString();

            try
            {
                Environment.SetEnvironmentVariable(AesCryptoSecretValueManager.DefaultEncryptionKeyId, keyId);
                Environment.SetEnvironmentVariable(keyId, "3H0vEYkSMENnRFrsSDEpWrzig+Hu05RnyL3hNEkzv6Q=");

                // Create our test input key
                var testInputKey = new Key { Name = "Test", Value = "Test secret value" };

                // Encrypt the key
                var resultKey = valueManager.WriteKeyValue(testInputKey);

                // Decrypt the encrypted key
                string decryptedSecret = valueManager.ReadKeyValue(resultKey);

                Assert.Equal(testInputKey.Value, decryptedSecret);
                Assert.Equal(keyId, resultKey.KeyId);
            }
            finally
            {
                Environment.SetEnvironmentVariable(keyId, null);
                Environment.SetEnvironmentVariable(AesCryptoSecretValueManager.DefaultEncryptionKeyId, null);
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

        private void TestKeyConfigurationException(Action<AesCryptoSecretValueManager, Key> keyOperation, string keyId = null)
        {
            var valueManager = new AesCryptoSecretValueManager();

            // Create our test input key
            var testInputKey = new Key { Name = "Test", Value = "Test secret value", KeyId = keyId };

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => keyOperation(valueManager, testInputKey));

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
