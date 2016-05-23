// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class SimpleBindingArgumentConverter : IBindingArgumentConverter
    {
        public bool CanConvert(Type argumentType, DataType functionDataType)
        {
            return IsStringConversion(argumentType, functionDataType) || IsBinaryConversion(argumentType, functionDataType);
        }

        public Task<object> ConvertFromValueAsync(Type argumentType, object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            // If this was a byte[], just return the value; otherwise, we'll return the IAsyncCollector instance
            // with the added value
            return Task.FromResult(value);
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

        private static bool IsBinaryConversion(Type argumentType, DataType functionDataType) =>
            functionDataType == DataType.Binary && (argumentType == typeof(byte[]) || typeof(IAsyncCollector<byte[]>).IsAssignableFrom(argumentType));

        private static bool IsStringConversion(Type argumentType, DataType functionDataType) =>
            functionDataType == DataType.String && (argumentType == typeof(string) || typeof(IAsyncCollector<string>).IsAssignableFrom(argumentType));
    }
}
