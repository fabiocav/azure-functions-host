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
    internal sealed class ScriptSecretSerializerV1 : IScriptSecretSerializer
    {
        private const string FunctionKeysPropertyName = "keys";
        private const string VersionPropertyName = "version";

        public int SupportedFormatVersion => 1;

        public IList<Key> DeserializeFunctionSecrets(JObject secrets)
        {
            return secrets.Property(FunctionKeysPropertyName)?.Value.ToObject<List<Key>>();
        }

        public HostSecrets DeserializeHostSecrets(JObject secrets)
        {
            return secrets.ToObject<HostSecrets>();
        }

        public string SerializeFunctionSecrets(IList<Key> secrets)
        {
            var functionSecrets = new JObject
            {
                [FunctionKeysPropertyName] = JToken.FromObject(secrets),
                [VersionPropertyName] = SupportedFormatVersion
            };

            return functionSecrets.ToString(Formatting.Indented);
        }

        public string SerializeHostSecrets(HostSecrets secrets)
        {
            return JsonConvert.SerializeObject(secrets, Formatting.Indented);
        }
    }
}
