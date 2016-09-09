// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public sealed class DefaultKeyValueConverterFactory : IKeyValueConverterFactory
    {
        private bool _encryptionSupported;

        public DefaultKeyValueConverterFactory()
        {
            _encryptionSupported = Environment.GetEnvironmentVariable(AesCryptoKeyValueConverter.DefaultEncryptionKeyId) != null;
        }

        public IKeyValueConverter GetValueConverter(Key key, KeyConversionAction action)
        {
            if (action == KeyConversionAction.Read)
            {
                return GetReadValueConverter(key);
            }

            return GetWriteValueConverter(key);
        }

        private IKeyValueConverter GetWriteValueConverter(Key key)
        {
            if (_encryptionSupported)
            {
                return new AesCryptoKeyValueConverter();
            }

            return new PlainTextKeyValueConverter();
        }

        private IKeyValueConverter GetReadValueConverter(Key key)
        {
            if (key.IsEncrypted)
            {
                return new AesCryptoKeyValueConverter();
            }

            return new PlainTextKeyValueConverter();
        }
    }
}
