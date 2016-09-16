// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.WebHost;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests.Security
{
    public class ScriptTokenUtilityTests
    {
        [Fact]
        public void CreateToken_SetsExpectedClaims()
        {
            try
            {
                ScriptCryptoUtility.SigningKeyResolver = () => "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";

                var tokenString = ScriptCryptoUtility.CreateScriptToken();

                var token = new JwtSecurityToken(tokenString);

                Assert.Equal("azure:functions", token.Issuer);
                Assert.Equal("azure:functions", token.Audiences.First());
                Assert.Equal(token.ValidFrom.AddSeconds(30), token.ValidTo);
            }
            finally
            {
                ScriptCryptoUtility.SigningKeyResolver = null;
            }
        }

        [Fact]
        public void ValidateToken_SucceedsWithValidToken()
        {
            try
            {
                ScriptCryptoUtility.SigningKeyResolver = () => "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";

                var tokenString = ScriptCryptoUtility.CreateScriptToken();

                var token = ScriptCryptoUtility.ValidateScriptToken(tokenString);

                Assert.Equal("azure:functions", token.Issuer);
                Assert.Equal(token.ValidFrom.AddSeconds(30), token.ValidTo);
            }
            finally
            {
                ScriptCryptoUtility.SigningKeyResolver = null;
            }
        }

        [Fact]
        public void ValidateToken_WithMismatchedKeys_Fails()
        {
            try
            {
                ScriptCryptoUtility.SigningKeyResolver = () => "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";

                var tokenString = ScriptCryptoUtility.CreateScriptToken();

                // Change key
                ScriptCryptoUtility.SigningKeyResolver = () => "1F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";

                Assert.ThrowsAny<SecurityTokenValidationException>(() => ScriptCryptoUtility.ValidateScriptToken(tokenString));
            }
            finally
            {
                ScriptCryptoUtility.SigningKeyResolver = null;
            }
        }

        [Fact]
        public void ValidateToken_WithTamperedToken_Fails()
        {
            try
            {
                ScriptCryptoUtility.SigningKeyResolver = () => "0F75CA46E7EBDD39E4CA6B074D1F9A5972B849A55F91A248";

                var tokenString = ScriptCryptoUtility.CreateScriptToken();

                var token = new JwtSecurityToken(tokenString);

                token.Payload["aud"] = "other";

                tokenString = new JwtSecurityTokenHandler().WriteToken(token) + token.RawSignature;

                var tamperedToken = new JwtSecurityToken(tokenString);

                Assert.Equal(token.RawHeader, tamperedToken.RawHeader);
                Assert.NotEqual(token.RawPayload, tamperedToken.RawPayload);
                Assert.Equal(token.RawSignature, tamperedToken.RawSignature);

                Assert.ThrowsAny<SecurityTokenInvalidSignatureException>(() => ScriptCryptoUtility.ValidateScriptToken(tokenString));
            }
            finally
            {
                ScriptCryptoUtility.SigningKeyResolver = null;
            }
        }
    }
}
