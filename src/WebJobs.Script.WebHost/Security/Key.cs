// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class Key : IEquatable<Key>
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "encrypted")]
        public bool IsEncrypted { get; set; }

        [JsonProperty(PropertyName = "encryptionKeyId")]
        public string EncryptionKeyId { get; set; }

        public bool Equals(Key other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Equals(Name, other.Name, StringComparison.Ordinal) &&
                string.Equals(Value, other.Value, StringComparison.Ordinal) &&
                string.Equals(EncryptionKeyId, other.EncryptionKeyId, StringComparison.Ordinal) &&
                IsEncrypted == other.IsEncrypted;
        }

        public override bool Equals(object obj) => Equals(obj as Key);

        public override int GetHashCode() => GetPropertyHashCode(Name) ^ GetPropertyHashCode(Value) ^ GetPropertyHashCode(EncryptionKeyId) ^ IsEncrypted.GetHashCode();

        private static int GetPropertyHashCode(string value) => value?.GetHashCode() ?? 0;
    }
}
