// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public sealed class StreamBindingArgumentConverter : IBindingArgumentConverter
    {
        private static readonly DataType[] SupportedDataTypes = new[] { DataType.Stream, DataType.String, DataType.Binary };
        public bool CanConvert(Type argumentType, DataType functionDataType)
        {
            bool isSupported = false;
            if (typeof(Stream).IsAssignableFrom(argumentType))
            {
                isSupported = SupportedDataTypes.Contains(functionDataType);
            }

            return isSupported;
        }

        public async Task<object> ConvertFromValueAsync(Type argumentType, object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            ValidateConversion(valueType, argumentType);

            Stream inputStream = context.BindingArguments
                .Where(b => string.Compare(b.Binding.Metadata.Name, binding.Metadata.Name) == 0)
                .FirstOrDefault()?.Value as Stream;
            
            // If the target is a stream but one wasn't provided, create a new one
            inputStream = inputStream ?? new MemoryStream();

            Stream valueStream = value as Stream;
            if (valueStream == null)
            {
                // Convert the value to bytes and write it
                // to the stream
                byte[] bytes = null;
                Type type = value.GetType();
                if (type == typeof(byte[]))
                {
                    bytes = (byte[])value;
                }
                else if (type == typeof(string))
                {
                    bytes = Encoding.UTF8.GetBytes((string)value);
                }

                using (valueStream = new MemoryStream(bytes))
                {
                    await valueStream.CopyToAsync(inputStream);
                }
            }
            else
            {
                // value is already a stream, so copy it directly
                await valueStream.CopyToAsync(inputStream);
            }

            return inputStream;
        }

        public async Task<T> ConvertFromValueAsync<T>(object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            var result = await ConvertFromValueAsync(typeof(T), value, valueType, binding, context);

            return (T)result;
        }

        public Task<object> ConvertToValueAsync(DataType valueType, object argument, FunctionBinding binding, InvocationContext context)
        {
            ValidateConversion(valueType, argument?.GetType());

            Stream stream = (Stream)argument;
            object result = null;
            switch (valueType)
            {
                case DataType.String:
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                    }
                    break;
                case DataType.Binary:
                    using (MemoryStream ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        result = ms.ToArray();
                    }
                    break;
                case DataType.Stream:
                    // when the target value is a Stream, we copy the value
                    // into the Stream passed in
                    Stream targetStream = result as Stream;
                    stream.CopyTo(targetStream);
                    break;
            }

            return Task.FromResult(result);
        }

        private void ValidateConversion(DataType valueType, Type argumentType)
        {
            if (!CanConvert(argumentType, valueType))
            {
                throw new InvalidOperationException($"Conversion between type {argumentType.Name} and data type {valueType.ToString("G")} is not supported.");
            }
        }
    }
}
