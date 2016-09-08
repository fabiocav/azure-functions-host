// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public sealed class DefaultSecretValueManagerFactory : ISecretValueManagerFactory
    {
        private bool _encryptionSupported;

        public DefaultSecretValueManagerFactory()
        {
            _encryptionSupported = Environment.GetEnvironmentVariable(AesCryptoSecretValueManager.DefaultEncryptionKeyId) != null;
        }

        public ISecretValueManager GetValueManager(Key key, SecretCryptoAction action)
        {
            if (action == SecretCryptoAction.Read)
            {
                return GetReadValueManager(key);
            }

            return GetWriteValueManager(key);
        }

        private ISecretValueManager GetWriteValueManager(Key key)
        {
            if (_encryptionSupported)
            {
                return new AesCryptoSecretValueManager();
            }

            return new PlainTextSecretValueManager();
        }

        private ISecretValueManager GetReadValueManager(Key key)
        {
            if (key.IsEncrypted)
            {
                return new AesCryptoSecretValueManager();
            }

            return new PlainTextSecretValueManager();
        }
    }
}
