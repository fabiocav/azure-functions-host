// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.WebHost;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests.Security
{
    public class ScriptSecretSerializerV1Tests
    {
        [Fact]
        public void SerializeFunctionSecrets_ReturnsExpectedResult()
        {
            var serializer = new ScriptSecretSerializerV1();

            var secrets = new List<Key>
            {
                new Key
                {
                    Name = "Key1",
                    Value = "Value1",
                    IsEncrypted = false,
                    EncryptionKeyId = "KeyId1"
                },
                new Key
                {
                    Name = "Key2",
                    Value = "Value2",
                    IsEncrypted = true,
                    EncryptionKeyId = "KeyId2"
                }
            };

            string serializedSecret = serializer.SerializeFunctionSecrets(secrets);

            var jsonObject = JObject.Parse(serializedSecret);
            var serializedSecrets = jsonObject.Property("keys")?.Value?.ToObject<List<Key>>();

            Assert.Equal(1, jsonObject.Value<int>("version"));
            Assert.NotNull(serializedSecret);
            AssertKeyCollectionsEquality(secrets, serializedSecrets);
        }

        [Fact]
        public void DeserializeFunctionSecrets_ReturnsExpectedResult()
        {
            var serializer = new ScriptSecretSerializerV1();
            var serializedSecret = "{ 'keys': [ { 'name': 'Key1', 'value': 'Value1', 'encrypted': false, 'encryptionKeyId': 'KeyId1' }, { 'name': 'Key2', 'value': 'Value2', 'encrypted': true, 'encryptionKeyId': 'KeyId2' } ], 'version': 1}";
            var expected = new List<Key>
            {
                new Key
                {
                    Name = "Key1",
                    Value = "Value1",
                    IsEncrypted = false,
                    EncryptionKeyId = "KeyId1"
                },
                new Key
                {
                    Name = "Key2",
                    Value = "Value2",
                    IsEncrypted = true,
                    EncryptionKeyId = "KeyId2"
                }
            };

            IList<Key> actual = serializer.DeserializeFunctionSecrets(JObject.Parse(serializedSecret));
            AssertKeyCollectionsEquality(expected, actual);
        }

        [Fact]
        public void DeserializeHostSecrets_ReturnsExpectedResult()
        {
            var serializer = new ScriptSecretSerializerV1();
            var serializedSecret = "{'masterKey':{'name':'master','value':'1234','encrypted':false,'encryptionKeyId':null},'functionKeys':[{'name':'Key1','value':'Value1','encrypted':false,'encryptionKeyId':'KeyId1'},{'name':'Key2','value':'Value2','encrypted':true,'encryptionKeyId':'KeyId2'}],'version':1}";
            var expected = new HostSecrets
            {
                MasterKey = new Key { Name = "master", Value = "1234" },
                FunctionKeys = new List<Key>
                {
                    new Key
                    {
                        Name = "Key1",
                        Value = "Value1",
                        IsEncrypted = false,
                        EncryptionKeyId = "KeyId1"
                    },
                    new Key
                    {
                        Name = "Key2",
                        Value = "Value2",
                        IsEncrypted = true,
                        EncryptionKeyId = "KeyId2"
                    }
                }
            };

            HostSecrets actual = serializer.DeserializeHostSecrets(JObject.Parse(serializedSecret));

            Assert.NotNull(actual);
            Assert.Equal(expected.MasterKey, actual.MasterKey);
            AssertKeyCollectionsEquality(expected.FunctionKeys, actual.FunctionKeys);
        }

        [Fact]
        public void SerializeHostSecrets_ReturnsExpectedResult()
        {
            var serializer = new ScriptSecretSerializerV1();

            var secrets = new HostSecrets
            {
                MasterKey = new Key { Name = "master", Value = "1234" },
                FunctionKeys = new List<Key>
                {
                    new Key
                    {
                        Name = "Key1",
                        Value = "Value1",
                        IsEncrypted = false,
                        EncryptionKeyId = "KeyId1"
                    },
                    new Key
                    {
                        Name = "Key2",
                        Value = "Value2",
                        IsEncrypted = true,
                        EncryptionKeyId = "KeyId2"
                    }
                }
            };

            string serializedSecret = serializer.SerializeHostSecrets(secrets);

            var jsonObject = JObject.Parse(serializedSecret);
            var functionSecrets = jsonObject.Property("functionKeys")?.Value?.ToObject<List<Key>>();
            var masterKey = jsonObject.Property("masterKey")?.Value?.ToObject<Key>();

            Assert.Equal(1, jsonObject.Value<int>("version"));
            Assert.NotNull(serializedSecret);
            Assert.Equal(secrets.MasterKey, masterKey);
            AssertKeyCollectionsEquality(secrets.FunctionKeys, functionSecrets);
        }

        private void AssertKeyCollectionsEquality(IList<Key> expected, IList<Key> actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Count, actual.Count);
            Assert.True(expected.Zip(actual, (k1, k2) => k1.Equals(k2)).All(r => r));
        }
    }
}
