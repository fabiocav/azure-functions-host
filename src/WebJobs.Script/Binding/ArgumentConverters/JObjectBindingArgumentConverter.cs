// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class JObjectBindingArgumentConverter : IBindingArgumentConverter
    {
        public bool CanConvert(Type argumentType, DataType functionDataType)
        {
            return functionDataType == DataType.Object &&
                (argumentType == typeof(JObject) || typeof(IAsyncCollector<JObject>).IsAssignableFrom(argumentType));
        }

        public Task<object> ConvertFromValueAsync(Type argumentType, object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            object result = value;

            if (value is ExpandoObject)
            {
                result = JObject.FromObject(value);
            }

            return Task.FromResult(result);
        }

        public async Task<T> ConvertFromValueAsync<T>(object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            var result = await ConvertFromValueAsync(typeof(T), value, valueType, binding, context);
            return (T)result;
        }

        public Task<object> ConvertToValueAsync(DataType valueType, object argument, FunctionBinding binding, InvocationContext context)
        {
            // Currently, this converter just returns the raw value. We need to enhance this to give script
            // invokers the ability to write to a file or input
            return Task.FromResult(argument);
        }
    }
}
