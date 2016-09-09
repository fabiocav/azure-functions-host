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
    internal sealed class ScriptSecretReader
    {
        private static List<IScriptSecretSerializer> _secretFormatters = new List<IScriptSecretSerializer>
        {
            new ScriptSecretSerializerV0(),
            new ScriptSecretSerializerV1()
        };

        private static IScriptSecretSerializer DefaultSerializer => GetSerializer(_secretFormatters.Max(i => i.SupportedFormatVersion));

        public IList<Key> ReadFunctionSecrets(string secretsJson)
        {
            var secretsObject = JObject.Parse(secretsJson);

            IScriptSecretSerializer serializer = GetSerializer(secretsObject);
            return serializer.DeserializeFunctionSecrets(secretsObject);
        }

        public HostSecrets ReadHostSecrets(string secretsJson)
        {
            var secrets = JObject.Parse(secretsJson);

            IScriptSecretSerializer serializer = GetSerializer(secrets);
            return serializer.DeserializeHostSecrets(secrets);
        }

        public string WriteFunctionSecrets(IList<Key> secrets) => DefaultSerializer.SerializeFunctionSecrets(secrets);

        public string WriteHostSecrets(HostSecrets secrets) => DefaultSerializer.SerializeHostSecrets(secrets);

        private static IScriptSecretSerializer GetSerializer(JObject secrets)
        {
            int formatVersion = secrets.Property("version")?.Value<int>() ?? 0;

            return GetSerializer(formatVersion);
        }

        private static IScriptSecretSerializer GetSerializer(int formatVersion)
        {
            IScriptSecretSerializer serializer = _secretFormatters.FirstOrDefault(s => s.SupportedFormatVersion == formatVersion);

            if (serializer == null)
            {
                throw new FormatException("Invalid function secrets file format.");
            }

            return serializer;
        }
    }
}
