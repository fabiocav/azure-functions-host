// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    internal class ScriptSecretSerializerV0 : IScriptSecretSerializer
    {
        private const string MasterKeyPropertyName = "masterKey";
        private const string HostFunctionKeyPropertyName = "functionKey";
        private const string FunctionKeyPropertyName = "key";

        public int SupportedFormatVersion => 0;

        public IList<Key> DeserializeFunctionSecrets(JObject secrets)
        {
            string key = secrets.Value<string>(FunctionKeyPropertyName);

            return new List<Key> { CreateKeyFromSecret(key) };
        }

        public HostSecrets DeserializeHostSecrets(JObject secrets)
        {
            string masterSecret = secrets.Value<string>(MasterKeyPropertyName);
            string functionSecret = secrets.Value<string>(HostFunctionKeyPropertyName);

            return new HostSecrets
            {
                MasterKey = CreateKeyFromSecret(masterSecret),
                FunctionKeys = new List<Key> { CreateKeyFromSecret(functionSecret) }
            };
        }

        public string SerializeFunctionSecrets(IList<Key> secrets)
        {
            var functionSecrets = new JObject
            {
                [FunctionKeyPropertyName] = secrets.FirstOrDefault(s => string.IsNullOrEmpty(s.Name))?.Value
            };

            return functionSecrets.ToString();
        }

        public string SerializeHostSecrets(HostSecrets secrets)
        {
            string functionKey = secrets.FunctionKeys
                ?.FirstOrDefault(k => string.IsNullOrEmpty(k.Name))
                ?.Value;

            var hostSecrets = new JObject
            {
                [MasterKeyPropertyName] = secrets.MasterKey.Value,
                [HostFunctionKeyPropertyName] = functionKey
            };

            return hostSecrets.ToString();
        }

        private static Key CreateKeyFromSecret(string secret)
        {
            return new Key { Name = string.Empty, Value = secret };
        }
    }
}
