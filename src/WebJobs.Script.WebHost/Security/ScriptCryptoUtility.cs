// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public static class ScriptCryptoUtility
    {
        private const string Issuer = "azure:functions";
        private const string Audience = "azure:functions";
        private const string MachingKeyXPathFormat = "configuration/location[@path='{0}']/system.web/machineKey/@{1}";

        public enum MachineKeyType
        {
            Encryption,
            Signing
        }

        internal static Func<string> SigningKeyResolver { get; set; }

        public static string CreateScriptToken()
        {
            var handler = new JwtSecurityTokenHandler();
            var credentials = new SigningCredentials(new SymmetricSecurityKey(GetSigningKey()), SecurityAlgorithms.HmacSha256Signature);

            return handler.CreateEncodedJwt(Issuer,
                Audience,
                subject: null,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddSeconds(30),
                issuedAt: DateTime.UtcNow,
                signingCredentials: credentials);
        }

        public static SecurityToken ValidateScriptToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(GetSigningKey()),
                ValidAudience = Audience,
                ValidIssuer = Issuer
            };

            SecurityToken resultingToken;
            handler.ValidateToken(token, validationParameters, out resultingToken);

            return resultingToken;
        }

        private static byte[] GetSigningKey()
        {
            string keyValue = SigningKeyResolver?.Invoke() ?? GetMachineConfigKey(@"D:\local\config\rootweb.config", MachineKeyType.Signing);

            if (keyValue == null)
            {
                throw new System.Configuration.ConfigurationErrorsException("Unable to retrieve signing key.");
            }

            return ConvertHexToByteArray(keyValue);
        }

        private static string GetMachineConfigKey(string configPath, MachineKeyType keyType, string siteName = null)
        {
            siteName = siteName ?? Environment.GetEnvironmentVariable(EnvironmentSettingNames.AzureWebsiteName);
            string keyAttribute = keyType == MachineKeyType.Encryption ? "decryptionKey" : "validationKey";

            using (var reader = new StringReader(configPath))
            {
                var xdoc = XDocument.Load(reader);

                string xpath = string.Format(CultureInfo.InvariantCulture, MachingKeyXPathFormat, siteName, keyAttribute);

                return ((IEnumerable)xdoc.XPathEvaluate(xpath)).Cast<XAttribute>().FirstOrDefault()?.Value;
            }
        }

        public static byte[] ConvertHexToByteArray(string keyValue)
           => Enumerable.Range(0, keyValue.Length / 2)
            .Select(b => Convert.ToByte(keyValue.Substring(b * 2, 2), 16))
            .ToArray();
    }
}
