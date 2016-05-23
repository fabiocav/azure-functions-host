// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script
{
    public static class Utility
    {
        public static string GetFunctionShortName(string functionName)
        {
            int idx = functionName.LastIndexOf('.');
            if (idx > 0)
            {
                functionName = functionName.Substring(idx + 1);
            }

            return functionName;
        }

        public static string FlattenException(Exception ex)
        {
            StringBuilder flattenedErrorsBuilder = new StringBuilder();
            string lastError = null;

            if (ex is AggregateException)
            {
                ex = ex.InnerException;
            }

            do
            {
                StringBuilder currentErrorBuilder = new StringBuilder();
                if (!string.IsNullOrEmpty(ex.Source))
                {
                    currentErrorBuilder.AppendFormat("{0}: ", ex.Source);
                }

                currentErrorBuilder.Append(ex.Message);

                if (!ex.Message.EndsWith("."))
                {
                    currentErrorBuilder.Append(".");
                }

                // sometimes inner exceptions are exactly the same
                // so first check before duplicating
                string currentError = currentErrorBuilder.ToString();
                if (lastError == null ||
                    string.Compare(lastError.Trim(), currentError.Trim()) != 0)
                {
                    if (flattenedErrorsBuilder.Length > 0)
                    {
                        flattenedErrorsBuilder.Append(" ");
                    }
                    flattenedErrorsBuilder.Append(currentError);
                }

                lastError = currentError;
            }
            while ((ex = ex.InnerException) != null);

            return flattenedErrorsBuilder.ToString();
        }

        public static string GetAppSettingOrEnvironmentValue(string name)
        {
            // first check app settings
            string value = ConfigurationManager.AppSettings[name];
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Check environment variables
            value = Environment.GetEnvironmentVariable(name);
            if (value != null)
            {
                return value;
            }

            return null;
        }

        public static bool IsJson(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            input = input.Trim();
            return (input.StartsWith("{", StringComparison.OrdinalIgnoreCase) && input.EndsWith("}", StringComparison.OrdinalIgnoreCase))
                || (input.StartsWith("[", StringComparison.OrdinalIgnoreCase) && input.EndsWith("]", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// If the specified input is a JSON string or JToken, attempt to deserialize it into
        /// an object or array.
        /// </summary>
        public static bool TryConvertJson(object input, out object result, params JsonConverter[] converters)
        {
            if (input is JToken)
            {
                input = input.ToString();
            }

            result = null;
            string inputString = input as string;
            if (inputString == null)
            {
                return false;
            }

            if (Utility.IsJson(inputString))
            {
                // if the input is json, try converting to an object or array
                Dictionary<string, object> jsonObject;
                Dictionary<string, object>[] jsonObjectArray;
                if (TryDeserializeJson(inputString, out jsonObject))
                {
                    result = jsonObject;
                    return true;
                }
                else if (TryDeserializeJson(inputString, out jsonObjectArray, converters))
                {
                    result = jsonObjectArray;
                    return true;
                }
            }

            return false;
        }

        public static bool TryDeserializeJson<TResult>(string json, out TResult result, params JsonConverter[] converters)
        {
            result = default(TResult);

            try
            {
                result = JsonConvert.DeserializeObject<TResult>(json, converters);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
