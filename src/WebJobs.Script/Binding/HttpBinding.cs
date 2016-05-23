// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class HttpBinding : FunctionBinding, IResultProcessingBinding
    {
        public HttpBinding(ScriptHostConfiguration config, BindingMetadata metadata, FileAccess access) : 
            base(config, metadata, access)
        {
        }

        public override Type GetArgumentType()
        {
            return typeof(HttpRequestMessage);
        }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            return null;
        }
        
        public void ProcessResult(IDictionary<string, object> functionArguments, object[] systemArguments, string triggerInputName, object result)
        {
            if (result == null)
            {
                return;
            }

            HttpRequestMessage request = null;
            object argValue = null;
            if (functionArguments.TryGetValue(triggerInputName, out argValue) && argValue is HttpRequestMessage)
            {
                request = (HttpRequestMessage)argValue;
            }
            else
            {
                // No argument is bound to the request message, so we should have 
                // it in the system arguments
                request = systemArguments.FirstOrDefault(a => a is HttpRequestMessage) as HttpRequestMessage;
            }

            if (request != null)
            {
                HttpResponseMessage response = result as HttpResponseMessage;
                if (response == null)
                {
                    response = request.CreateResponse(HttpStatusCode.OK);
                    response.Content = new ObjectContent(result.GetType(), result, new JsonMediaTypeFormatter());
                }

                request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = response;
            }
        }

        public bool CanProcessResult(object result)
        {
            return result != null;
        }
    }
}
