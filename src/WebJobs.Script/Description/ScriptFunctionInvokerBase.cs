// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.Azure.WebJobs.Script.Binding;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    [CLSCompliant(false)]
    public class ScriptFunctionInvokerBase : FunctionInvokerBase
    {
        private readonly List<IBindingArgumentConverter> _argumentConverters;

        public ScriptFunctionInvokerBase(ScriptHost host, FunctionMetadata functionMetadata) : base(host, functionMetadata)
        {
            // This needs to be moved. Here to prototype the invoker changes
            _argumentConverters = new List<IBindingArgumentConverter>
            {
                new StreamBindingArgumentConverter(),
                new HttpBindingArgumentConverter(),
                new JObjectBindingArgumentConverter(),
                new SimpleBindingArgumentConverter(),
                new TimerInfoBindingArgumentConverter()
            };
        }

        public override Task Invoke(object[] parameters)
        {
            throw new System.NotImplementedException();
        }

        protected async Task ProcessInputBindingsAsync(object input, string functionInstanceOutputPath, IBinderEx binder,
            Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings,
            Dictionary<string, string> bindingData, Dictionary<string, string> environmentVariables)
        {
            // if there are any input or output bindings declared, set up the temporary
            // output directory
            if (outputBindings.Count > 0 || inputBindings.Any())
            {
                Directory.CreateDirectory(functionInstanceOutputPath);
            }

            // process input bindings
            foreach (var inputBinding in inputBindings)
            {
                string filePath = Path.Combine(functionInstanceOutputPath, inputBinding.Metadata.Name);
                using (FileStream stream = File.OpenWrite(filePath))
                {
                    // If this is the trigger input, write it directly to the stream.
                    // The trigger binding is a special case because it is early bound
                    // rather than late bound as is the case with all the other input
                    // bindings.
                    if (inputBinding.Metadata.IsTrigger)
                    {
                        if (input is string)
                        {
                            using (StreamWriter sw = new StreamWriter(stream))
                            {
                                await sw.WriteAsync((string)input);
                            }
                        }
                        else if (input is byte[])
                        {
                            byte[] bytes = input as byte[];
                            await stream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        else if (input is Stream)
                        {
                            Stream inputStream = input as Stream;
                            await inputStream.CopyToAsync(stream);
                        }
                    }
                    else
                    {
                        // invoke the input binding
                        BindingContext bindingContext = new BindingContext
                        {
                            Binder = binder,
                            BindingData = bindingData,
                            DataType = DataType.Stream, 
                            Value = stream
                        };

                        // TODO: (INVOKERWORK) Fix this....
                        //await inputBinding.BindAsync(bindingContext);
                    }
                }

                environmentVariables[inputBinding.Metadata.Name] = Path.Combine(functionInstanceOutputPath,
                    inputBinding.Metadata.Name);
            }
        }

        private async Task ProcessInputParametersAsync(InvocationContext context, string functionInstanceOutputPath)
        {
            IDictionary<string, string> environmentVariables = context.ExecutionContext["environmentVariables"] as IDictionary<string, string>;

            if (environmentVariables == null)
            {
                environmentVariables = new Dictionary<string, string>();
                context.ExecutionContext["environmentVariables"] = environmentVariables;
            }

            foreach (var argument in context.BindingArguments.Where(b => b.Binding.Metadata.Direction == BindingDirection.In))
            {
                string filePath = Path.Combine(functionInstanceOutputPath, argument.Binding.Metadata.Name);
                using (FileStream stream = File.OpenWrite(filePath))
                {
                    DataType dataType = argument.Binding.Metadata.DataType ?? DataType.String;
                    IBindingArgumentConverter converter = _argumentConverters.FirstOrDefault(c => c.CanConvert(argument.Value.GetType(), dataType));
                    
                    if (converter != null)
                    {
                        object input = await converter.ConvertToValueAsync(dataType, argument.Value, argument.Binding, context);

                        if (input is string)
                        {
                            using (StreamWriter sw = new StreamWriter(stream))
                            {
                                await sw.WriteAsync((string)input);
                            }
                        }
                        else if (input is byte[])
                        {
                            byte[] bytes = input as byte[];
                            await stream.WriteAsync(bytes, 0, bytes.Length);
                        }
                        else if (input is Stream)
                        {
                            Stream inputStream = input as Stream;
                            await inputStream.CopyToAsync(stream);
                        }

                        environmentVariables[argument.Binding.Metadata.Name] = Path.Combine(functionInstanceOutputPath, argument.Binding.Metadata.Name);
                    }
                }
            }
        }

        private async Task ProcessFunctionOutputAsync(InvocationContext context, ICollection<FunctionBinding> outputBindings, string functionInstanceOutputPath)
        {
            var bindings = (Dictionary<string, object>)context.ExecutionContext["bindings"];

            // Special hadling of HTTP bindings.
            // If we have an HTTP output binding, create an argument for it (as it is not provided by the SDK)
            FunctionBinding httpOutputBinding = outputBindings.FirstOrDefault(b => string.Compare(b.Metadata.Type, "http", StringComparison.OrdinalIgnoreCase) == 0);
            if (httpOutputBinding != null)
            {
                context.BindingArguments.Add(new BindingArgument(httpOutputBinding, null));
            }

            foreach (var argument in context.BindingArguments.Where(a => a.Binding.Metadata.Direction == BindingDirection.Out))
            {
                string filePath = Path.Combine(functionInstanceOutputPath, argument.Binding.Metadata.Name);
                if (File.Exists(filePath))
                {
                    using (FileStream stream = File.OpenRead(filePath))
                    {
                        object output;
                        bindings.TryGetValue(argument.Binding.Metadata.Name, out output);

                        Type argumentType = argument.Value?.GetType() ?? argument.Binding.GetArgumentType();
                        DataType dataType = argument.Binding.Metadata.DataType ?? DataType.String;
                        IBindingArgumentConverter converter = _argumentConverters.FirstOrDefault(c => c.CanConvert(argumentType, dataType));

                        if (converter != null)
                        {
                            object convertedOutput = await converter.ConvertFromValueAsync(argumentType, output, dataType, argument.Binding, context);

                            if (convertedOutput != null)
                            {
                                Type outputType = convertedOutput.GetType();
                                Type inputType = argument.Value?.GetType();
                                var asyncCollectorType = typeof(IAsyncCollector<>).MakeGenericType(outputType);
                                if (asyncCollectorType.IsAssignableFrom(inputType))
                                {
                                    MethodInfo addMethod = argumentType.GetMethod(nameof(IAsyncCollector<object>.AddAsync), new[] { outputType, typeof(CancellationToken) });
                                    Task addAsyncTask = (Task)addMethod.Invoke(argument.Value, new[] { convertedOutput, CancellationToken.None });
                                    await addAsyncTask;
                                }
                                else
                                {
                                    argument.Value = convertedOutput;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected static async Task ProcessOutputBindingsAsync(string functionInstanceOutputPath, Collection<FunctionBinding> outputBindings,
            object input, IBinderEx binder, Dictionary<string, string> bindingData)
        {
            if (outputBindings == null)
            {
                return;
            }

            try
            {
                foreach (var outputBinding in outputBindings)
                {
                    string filePath = System.IO.Path.Combine(functionInstanceOutputPath, outputBinding.Metadata.Name);
                    if (File.Exists(filePath))
                    {
                        using (FileStream stream = File.OpenRead(filePath))
                        {
                            BindingContext bindingContext = new BindingContext
                            {
                                TriggerValue = input,
                                Binder = binder,
                                BindingData = bindingData,
                                Value = stream
                            };
                            // TODO: (INVOKERWORK) Fix this...
                            // await outputBinding.BindAsync(bindingContext);
                            await Task.CompletedTask;
                        }
                    }
                }
            }
            finally
            {
                // clean up the output directory
                if (outputBindings.Any() && Directory.Exists(functionInstanceOutputPath))
                {
                    Directory.Delete(functionInstanceOutputPath, recursive: true);
                }
            }
        }

        protected static object ConvertInput(object input)
        {
            if (input != null)
            {
                // perform any required input conversions
                HttpRequestMessage request = input as HttpRequestMessage;
                if (request != null)
                {
                    // TODO: Handle other content types? (E.g. byte[])
                    if (request.Content != null && request.Content.Headers.ContentLength > 0)
                    {
                        return ((HttpRequestMessage)input).Content.ReadAsStringAsync().Result;
                    }
                }
            }

            return input;
        }

        protected void InitializeEnvironmentVariables(Dictionary<string, string> environmentVariables, string functionInstanceOutputPath, object input, Collection<FunctionBinding> outputBindings, ExecutionContext executionContext)
        {
            environmentVariables["InvocationId"] = executionContext.InvocationId.ToString();

            foreach (var outputBinding in outputBindings)
            {
                environmentVariables[outputBinding.Metadata.Name] = Path.Combine(functionInstanceOutputPath, outputBinding.Metadata.Name);
            }

            Type triggerParameterType = input.GetType();
            if (triggerParameterType == typeof(HttpRequestMessage))
            {
                HttpRequestMessage request = (HttpRequestMessage)input;
                environmentVariables["REQ_METHOD"] = request.Method.ToString();

                Dictionary<string, string> queryParams = request.GetQueryNameValuePairs().ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
                foreach (var queryParam in queryParams)
                {
                    string varName = string.Format(CultureInfo.InvariantCulture, "REQ_QUERY_{0}", queryParam.Key.ToUpperInvariant());
                    environmentVariables[varName] = queryParam.Value;
                }

                foreach (var header in request.Headers)
                {
                    string varName = string.Format(CultureInfo.InvariantCulture, "REQ_HEADERS_{0}", header.Key.ToUpperInvariant());
                    environmentVariables[varName] = header.Value.First();
                }
            }
        }
    }
}
