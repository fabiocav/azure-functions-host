// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Azure.Web.DataProtection;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public sealed class AesCryptoKeyValueConverter : KeyValueConverter, IKeyValueWriter, IKeyValueReader
    {
        private readonly IDataProtector _dataProtector;

        public AesCryptoKeyValueConverter(FileAccess access)
            : base(access)
        {
            var provider = DataProtectionProvider.CreateAzureDataProtector();
            _dataProtector = provider.CreateProtector("function-secrets");
        }

        public string ReadValue(Key key)
        {
            ValidateAccess(FileAccess.Read);

            return _dataProtector.Unprotect(key.Value);
        }

        public Key WriteValue(Key key)
        {
            ValidateAccess(FileAccess.Write);

            string encryptedValue = _dataProtector.Protect(key.Value);

            return new Key
            {
                Name = key.Name,
                EncryptionKeyId = GetKeyIdFromPayload(encryptedValue),
                Value = encryptedValue,
                IsEncrypted = true
            };
        }

        private static string GetKeyIdFromPayload(string encryptedValue)
        {
            // Payload format details at:
            // https://docs.asp.net/en/latest/security/data-protection/implementation/authenticated-encryption-details.html

            byte[] encryptedPayload = AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(encryptedValue);

            if (encryptedValue.Length < 20)
            {
                throw new CryptographicException("Invalid cryptographic payload. Unable to extract key id.");
            }

            return new Guid(encryptedPayload.Skip(4).Take(16).ToArray()).ToString();
        }
    }
}
