// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.WebHooks;

namespace Microsoft.Azure.WebJobs.Script.WebHost.WebHooks
{
    public class DynamicWebHookReceiverConfig : IWebHookReceiverConfig
    {
        private readonly SecretManager _secretManager;

        public DynamicWebHookReceiverConfig(SecretManager secretManager)
        {
            _secretManager = secretManager;
        }

        public Task<string> GetReceiverConfigAsync(string name, string id)
        {
            // "id" will be the function name
            // we ignore the "name" parameter since we only allow a function
            // to be mapped to a single receiver
            string functionSecret = _secretManager.GetFunctionSecrets(id).Values.FirstOrDefault();
            return Task.FromResult(functionSecret);
        }
    }
}