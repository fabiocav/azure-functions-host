// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class HttpBinding : FunctionBinding, IResultProcessingBinding
    {
        public HttpBinding(ScriptHostConfiguration config, BindingMetadata metadata, FileAccess access) : 
            base(config, metadata, access)
        {
        }

        public override Type DefaultType
        {
            get
            {
                return typeof(HttpRequestMessage);
            }
        }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            return null;
        }

        internal override object ProcessScriptInput(object arg, IDictionary<string, object> functionContext)
        {
            var request = arg as HttpRequestMessage;
            if (request != null)
            {
                string rawBody = null;
                var requestObject = CreateRequestObject(request, out rawBody);
                object input = requestObject;

                if (rawBody != null)
                {
                    requestObject["rawBody"] = rawBody;
                    // TODO (INVUPD)
                    // bindDataInput = rawBody;
                }

                // If this is a WebHook function, the input should be the
                // request body
                HttpTriggerBindingMetadata httpBinding = this.Metadata as HttpTriggerBindingMetadata;
                if (httpBinding != null &&
                    !string.IsNullOrEmpty(httpBinding.WebHookType))
                {
                    input = requestObject["body"];

                    // make the entire request object available as well
                    // this is symmetric with context.res which we also support
                    functionContext.Add("req", requestObject);
                }

                return input;
            }

            return base.ProcessScriptInput(arg, functionContext);
        }

        internal override object ProcessScriptOutput(IEnumerable<BindingArgument> bindingArguments, object output, Dictionary<string, object> executionContext)
        {
            HttpRequestMessage request = bindingArguments.Where(b => b.Binding.Metadata.IsTrigger).FirstOrDefault().Input as HttpRequestMessage;

            if (request == null)
            {
                return null;
            }

            string content = null;
            if (output.GetType() == typeof(ExpandoObject) || output is Array)
            {
                content = JsonConvert.SerializeObject(output);
            }
            else if (output is string)
            {
                content = (string)output;
            }
            else
            {
                throw new InvalidOperationException(string.Format("Invalid value specified for binding '{0}'", Metadata.Name));
            }

            HttpResponseMessage response = null;
            try
            {
                // attempt to read the content as a JObject
                JObject jsonObject = JObject.Parse(content);

                // TODO: This logic needs to be made more robust
                // E.g. we might decide to use a Regex to determine if
                // the json is a response body or not
                if (jsonObject["body"] != null)
                {
                    HttpStatusCode statusCode = HttpStatusCode.OK;
                    if (jsonObject["status"] != null)
                    {
                        statusCode = (HttpStatusCode)jsonObject.Value<int>("status");
                    }

                    string body = jsonObject["body"].ToString();

                    response = new HttpResponseMessage(statusCode);
                    response.Content = new StringContent(body);

                    // we default the Content-Type here, but we override below with any
                    // Content-Type header the user might have set themselves
                    // TODO: rather than newing up an HttpResponseMessage investigate using
                    // request.CreateResponse, which should allow WebApi Content negotiation to
                    // take place.
                    if (Utility.IsJson(body))
                    {
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    // apply any user specified headers
                    JObject headers = (JObject)jsonObject["headers"];
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            AddResponseHeader(response, header);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // not a json response
            }

            if (response == null)
            {
                // if unable to parse a json response just send
                // the raw content
                response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(content)
                };
            }

            request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = response;

            return null;
        }

        private static Dictionary<string, object> CreateRequestObject(HttpRequestMessage request, out string rawBody)
        {
            rawBody = null;

            // TODO: need to provide access to remaining request properties
            Dictionary<string, object> requestObject = new Dictionary<string, object>();
            requestObject["originalUrl"] = request.RequestUri.ToString();
            requestObject["method"] = request.Method.ToString().ToUpperInvariant();
            requestObject["query"] = request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                // since HTTP headers are case insensitive, we lower-case the keys
                // as does Node.js request object
                headers.Add(header.Key.ToLowerInvariant(), header.Value.First());
            }
            requestObject["headers"] = headers;

            // if the request includes a body, add it to the request object 
            if (request.Content != null && request.Content.Headers.ContentLength > 0)
            {
                string body = request.Content.ReadAsStringAsync().Result;
                rawBody = body;
                MediaTypeHeaderValue contentType = request.Content.Headers.ContentType;
                Dictionary<string, object> jsonObject;
                if (contentType != null && contentType.MediaType == "application/json" &&
                    TryDeserializeJson(body, out jsonObject))
                {
                    // if the content - type of the request is json, deserialize into an object
                    requestObject["body"] = jsonObject;
                }
                else
                {
                    requestObject["body"] = body;
                }
            }

            return requestObject;
        }

        private static bool TryDeserializeJson<TResult>(string json, out TResult result)
        {
            result = default(TResult);

            try
            {
                result = JsonConvert.DeserializeObject<TResult>(json, new DictionaryJsonConverter());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override async Task BindAsync(BindingContext context)
        {
            HttpRequestMessage request = (HttpRequestMessage)context.TriggerValue;

            // TODO: Find a better place for this code
            string content = string.Empty;
            if (context.Value is Stream)
            {
                using (StreamReader streamReader = new StreamReader((Stream)context.Value))
                {
                    content = await streamReader.ReadToEndAsync();
                }
            }
            else if (context.Value is string)
            {
                content = (string)context.Value;
            }
            
            HttpResponseMessage response = null;
            try
            {
                // attempt to read the content as a JObject
                JObject jsonObject = JObject.Parse(content);

                // TODO: This logic needs to be made more robust
                // E.g. we might decide to use a Regex to determine if
                // the json is a response body or not
                if (jsonObject["body"] != null)
                {
                    HttpStatusCode statusCode = HttpStatusCode.OK;
                    if (jsonObject["status"] != null)
                    {
                        statusCode = (HttpStatusCode)jsonObject.Value<int>("status");
                    }

                    string body = jsonObject["body"].ToString();

                    response = new HttpResponseMessage(statusCode);
                    response.Content = new StringContent(body);

                    // we default the Content-Type here, but we override below with any
                    // Content-Type header the user might have set themselves
                    // TODO: rather than newing up an HttpResponseMessage investigate using
                    // request.CreateResponse, which should allow WebApi Content negotiation to
                    // take place.
                    if (Utility.IsJson(body))
                    {
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    }

                    // apply any user specified headers
                    JObject headers = (JObject)jsonObject["headers"];
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            AddResponseHeader(response, header);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // not a json response
            }

            if (response == null)
            {
                // if unable to parse a json response just send
                // the raw content
                response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(content)
                };
            }

            request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey] = response;
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

        private static void AddResponseHeader(HttpResponseMessage response, KeyValuePair<string, JToken> header)
        {
            if (header.Value != null)
            {
                DateTimeOffset dateTimeOffset;
                switch (header.Key.ToLowerInvariant())
                {
                    // The following content headers must be added to the response
                    // content header collection
                    case "content-type":
                        response.Content.Headers.ContentType = new MediaTypeHeaderValue(header.Value.ToString());
                        break;
                    case "content-length":
                        long contentLength;
                        if (long.TryParse(header.Value.ToString(), out contentLength))
                        {
                            response.Content.Headers.ContentLength = contentLength;
                        }
                        break;
                    case "content-disposition":
                        response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(header.Value.ToString());
                        break;
                    case "content-encoding":
                    case "content-language":
                    case "content-range":
                        response.Content.Headers.Add(header.Key, header.Value.ToString());
                        break;
                    case "content-location":
                        Uri uri;
                        if (Uri.TryCreate(header.Value.ToString(), UriKind.Absolute, out uri))
                        {
                            response.Content.Headers.ContentLocation = uri;
                        }
                        break;
                    case "content-md5":
                        response.Content.Headers.ContentMD5 = header.Value.Value<byte[]>();
                        break;
                    case "expires":
                        if (DateTimeOffset.TryParse(header.Value.ToString(), out dateTimeOffset))
                        {
                            response.Content.Headers.Expires = dateTimeOffset;
                        }
                        break;
                    case "last-modified":
                        if (DateTimeOffset.TryParse(header.Value.ToString(), out dateTimeOffset))
                        {
                            response.Content.Headers.LastModified = dateTimeOffset;
                        }
                        break;
                    default:
                        // All other headers are added directly to the response
                        response.Headers.Add(header.Key, header.Value.ToString());
                        break;
                }
            }
        }
    }
}
