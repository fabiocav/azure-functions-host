﻿using System;
using System.Collections.Generic;
using Microsoft.Azure.Web.Cryptography;
using Xunit;

namespace Web.Cryptography.Tests
{
    public class DefaultEncryptionKeyResolverTests
    {
        [Fact]
        public void ResolveKey_WithDefaultKeyInAzure_ResolvesEnvironmentKey()
        {
            string testKey = "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";
            CryptographicKey expectedKey = CryptographicKey.FromHexString(DefaultEncryptionKeyResolver.DefaultEncryptionKeyId, testKey);

            using (var testVariables = new TestScopedEnvironmentVariable(new Dictionary<string, string>
            {
                { Constants.AzureWebsiteInstanceId, "123" },
                { Constants.AzureWebsiteEncryptionKey, testKey },
            }))
            {
                var resolver = new DefaultEncryptionKeyResolver();
                CryptographicKey key = resolver.ResolveKey(null);

                Assert.NotNull(key);
                Assert.Equal(expectedKey.Id, key.Id);
                Assert.Equal(expectedKey.GetValue(), key.GetValue());
            }
        }

        //        [Fact]
        //        public void ResolveKey_WithDefaultKeyInAzureMissingVariable_ResolvesMachineKey()
        //        {
        //            string testKey = "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";
        //            string testRootWebConfig =
        //$@"<configuration>
        //  <location path=""testsite"">
        //    <system.web>
        //      <machineKey validationKey=""NONE"" decryptionKey=""{testKey}"" decryption=""AES"" />
        //    </system.web>
        //  </location>
        //</configuration>";

        //            CryptographicKey expectedKey = CryptographicKey.FromHexString(DefaultEncryptionKeyResolver.DefaultEncryptionKeyId, testKey);
        //            var fileSystem = new MockFileSystem();
        //            fileSystem.AddFile(@"D:\local\config\rootweb.config", new MockFileData(testRootWebConfig));

        //            using (var testVariables = new TestScopedEnvironmentVariable(new Dictionary<string, string>
        //            {
        //                { EnvironmentSettingNames.AzureWebsiteInstanceId, "123" },
        //                { EnvironmentSettingNames.AzureWebsiteName, "testsite" },
        //            }))
        //            {
        //                var resolver = new DefaultEncryptionKeyResolver(fileSystem);
        //                CryptographicKey key = resolver.ResolveKey(null);

        //                Assert.NotNull(key);
        //                Assert.Equal(expectedKey.Id, key.Id);
        //                Assert.Equal(expectedKey.GetValue(), key.GetValue());
        //            }
        //        }

        [Fact]
        public void ResolveKey_WithKeyId_ResolvesEnvinronmentKey()
        {
            string testKeyId = "testkey";
            string testKey = "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";
            CryptographicKey expectedKey = CryptographicKey.FromHexString(testKeyId, testKey);

            using (var testVariables = new TestScopedEnvironmentVariable(new Dictionary<string, string>
            {
                { DefaultEncryptionKeyResolver.DefaultEncryptionKeyId, testKeyId },
                { testKeyId, testKey },
            }))
            {
                var resolver = new DefaultEncryptionKeyResolver();
                CryptographicKey key = resolver.ResolveKey(null);

                Assert.NotNull(key);
                Assert.Equal(expectedKey.Id, key.Id);
                Assert.Equal(expectedKey.GetValue(), key.GetValue());
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ResolveKey_WithDefaultKeyLocal_ResolvesEnvironmentKey(string keyId)
        {
            string testKey = "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";
            CryptographicKey expectedKey = CryptographicKey.FromHexString(Constants.AzureWebsiteLocalEncryptionKey, testKey);

            using (var testVariables = new TestScopedEnvironmentVariable(Constants.AzureWebsiteLocalEncryptionKey, testKey))
            {
                var resolver = new DefaultEncryptionKeyResolver();
                CryptographicKey key = resolver.ResolveKey(keyId);

                Assert.NotNull(key);
                Assert.Equal(DefaultEncryptionKeyResolver.DefaultEncryptionKeyId, key.Id);
                Assert.Equal(expectedKey.GetValue(), key.GetValue());
            }
        }
    }
}
