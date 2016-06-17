// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EdgeJs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.Azure.WebJobs.Script.Binding;
using Microsoft.Azure.WebJobs.Script.Diagnostics;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    // TODO: make this internal
    public class NodeFunctionInvoker : FunctionInvokerBase
    {
        private readonly Collection<FunctionBinding> _inputBindings;
        private readonly Collection<FunctionBinding> _outputBindings;
        private readonly string _script;
        private readonly BindingMetadata _trigger;
        private readonly IMetricsLogger _metrics;
        private readonly List<IBindingArgumentConverter> _argumentConverters;

        private Func<object, Task<object>> _scriptFunc;
        private Func<object, Task<object>> _clearRequireCache;
        private static Func<object, Task<object>> _globalInitializationFunc;
        private static string _functionTemplate;
        private static string _clearRequireCacheScript;
        private static string _globalInitializationScript;

        static NodeFunctionInvoker()
        {
            _functionTemplate = ReadResourceString("functionTemplate.js");
            _clearRequireCacheScript = ReadResourceString("clearRequireCache.js");
            _globalInitializationScript = ReadResourceString("globalInitialization.js");

            Initialize();
        }

        internal NodeFunctionInvoker(ScriptHost host, BindingMetadata trigger, FunctionMetadata functionMetadata, Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings)
            : base(host, functionMetadata)
        {
            _trigger = trigger;
            string scriptFilePath = functionMetadata.ScriptFile.Replace('\\', '/');
            _script = string.Format(CultureInfo.InvariantCulture, _functionTemplate, scriptFilePath);
            _inputBindings = inputBindings;
            _outputBindings = outputBindings;
            _metrics = host.ScriptConfig.HostConfig.GetService<IMetricsLogger>();

            InitializeFileWatcherIfEnabled();

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

        /// <summary>
        /// Event raised whenever an unhandled Node exception occurs at the
        /// global level (e.g. an unhandled async exception).
        /// </summary>
        public static event UnhandledExceptionEventHandler UnhandledException;

        private static Func<object, Task<object>> GlobalInitializationFunc
        {
            get
            {
                if (_globalInitializationFunc == null)
                {
                    _globalInitializationFunc = Edge.Func(_globalInitializationScript);
                }
                return _globalInitializationFunc;
            }
        }

        private Func<object, Task<object>> ScriptFunc
        {
            get
            {
                if (_scriptFunc == null)
                {
                    // We delay create the script function so any syntax errors in
                    // the function will be reported to the Dashboard as an invocation
                    // error rather than a host startup error
                    _scriptFunc = Edge.Func(_script);
                }
                return _scriptFunc;
            }
        }

        private Func<object, Task<object>> ClearRequireCacheFunc
        {
            get
            {
                if (_clearRequireCache == null)
                {
                    _clearRequireCache = Edge.Func(_clearRequireCacheScript);
                }
                return _clearRequireCache;
            }
        }

        public override async Task Invoke(object[] parameters)
        {
            TraceWriter traceWriter = (TraceWriter)parameters[0];
            ExecutionContext functionExecutionContext = (ExecutionContext)parameters[1];
            IBinderEx binder = (IBinderEx)parameters[2];
            object triggerInput = parameters[3];
            string invocationId = functionExecutionContext.InvocationId.ToString();

            FunctionStartedEvent startedEvent = new FunctionStartedEvent(functionExecutionContext.InvocationId, Metadata);
            _metrics.BeginEvent(startedEvent);

            try
            {
                TraceWriter.Info(string.Format("Function started (Id={0})", invocationId));

                // Binding parameters, exclude system parameters
                var argumentBindings = _inputBindings.Union(_outputBindings);
                List<BindingArgument> bindingArguments = parameters.Skip(3)
                    .Zip(argumentBindings, (arg, binding) => new BindingArgument(binding, arg))
                    .ToList();
                
                var scriptExecutionContext = await CreateScriptInvocationContextAsync(triggerInput, bindingArguments, traceWriter, TraceWriter, functionExecutionContext);

                var invocationContext = new InvocationContext(bindingArguments, scriptExecutionContext);

                PopulateBindingData(invocationContext, invocationId, binder);

                object functionResult = await ScriptFunc(scriptExecutionContext);

                await ProcessFunctionOutputAsync(invocationContext);

                for (int i = 0; i < bindingArguments.Count; i++)
                {
                    if (bindingArguments[i].Binding.Metadata.Direction == BindingDirection.Out)
                    {
                        parameters[i] = bindingArguments[i].Value;
                    }
                }

                TraceWriter.Info(string.Format("Function completed (Success, Id={0})", invocationId));
            }
            catch
            {
                startedEvent.Success = false;
                TraceWriter.Error(string.Format("Function completed (Failure, Id={0})", invocationId));
                throw;
            }
            finally
            {
                _metrics.EndEvent(startedEvent);
            }
        }

        private static void PopulateBindingData(InvocationContext invocationContext, string invocationId, IBinderEx binder)
        {
            var bindingData = (Dictionary<string, string>)invocationContext.ExecutionContext["bindingData"];
            bindingData["InvocationId"] = invocationId;

            foreach (var item in binder.BindingContext.BindingData)
            {
                bindingData.Add(item.Key, item.Value?.ToString());
            }
        }

        private async Task ProcessInputParametersAsync(InvocationContext context, IDictionary<string, object> bindings)
        {
            var convertedInputs = new List<object>();

            foreach (var argument in context.BindingArguments.Where(b => b.Binding.Metadata.Direction == BindingDirection.In))
            {
                DataType dataType = argument.Binding.Metadata.DataType ?? DataType.String;
                IBindingArgumentConverter converter = _argumentConverters.FirstOrDefault(c => c.CanConvert(argument.Value.GetType(), dataType));
                // Process the input, giving the binding the ability to perform 
                // any required conversions and setup the context
                if (converter != null)
                {
                    object input = await converter.ConvertToValueAsync(dataType, argument.Value, argument.Binding, context);

                    bindings.Add(argument.Binding.Metadata.Name, input);
                    convertedInputs.Add(input);
                }
            }

            context.ExecutionContext["inputs"] = convertedInputs;
        }

        private async Task ProcessFunctionOutputAsync(InvocationContext context)
        {
            var bindings = (Dictionary<string, object>)context.ExecutionContext["bindings"];

            // Special hadling of HTTP bindings.
            // If we have an HTTP output binding, create an argument for it (as it is not provided by the SDK)
            FunctionBinding httpOutputBinding = _outputBindings.FirstOrDefault(b => string.Compare(b.Metadata.Type, "http", StringComparison.OrdinalIgnoreCase) == 0);
            if (httpOutputBinding != null)
            {
                context.BindingArguments.Add(new BindingArgument(httpOutputBinding, null));
            }

            foreach (var argument in context.BindingArguments.Where(a => a.Binding.Metadata.Direction == BindingDirection.Out))
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

        protected override void OnScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            if (_scriptFunc == null)
            {
                // we're not loaded yet, so nothing to reload
                return;
            }

            // The ScriptHost is already monitoring for changes to function.json, so we skip those
            string fileName = Path.GetFileName(e.Name);
            if (string.Compare(fileName, ScriptConstants.FunctionMetadataFileName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                // one of the script files for this function changed
                // force a reload on next execution
                _scriptFunc = null;

                // clear the node module cache
                ClearRequireCacheFunc(null).GetAwaiter().GetResult();

                TraceWriter.Info(string.Format(CultureInfo.InvariantCulture, "Script for function '{0}' changed. Reloading.", Metadata.Name));
            }
        }

        private async Task<Dictionary<string, object>> CreateScriptInvocationContextAsync(object triggerInput, List<BindingArgument> bindingArguments, TraceWriter traceWriter, 
            TraceWriter fileTraceWriter, ExecutionContext functionExecutionContext)
        {
            DataType dataType = _trigger.DataType ?? DataType.String;

            // create a TraceWriter wrapper that can be exposed to Node.js
            var log = (Func<object, Task<object>>)(p =>
            {
                string text = p as string;
                if (text != null)
                {
                    traceWriter.Info(text);
                    fileTraceWriter.Info(text);
                } 

                return Task.FromResult<object>(null);
            });

            var bindings = new Dictionary<string, object>();
            var bind = (Func<object, Task<object>>)(p =>
            {
                IDictionary<string, object> bindValues = (IDictionary<string, object>)p;
                foreach (var bindValue in bindValues)
                {
                    bindings[bindValue.Key] = bindValue.Value;
                }
                return Task.FromResult<object>(null);
            });

            var context = new Dictionary<string, object>()
            {
                { "invocationId", functionExecutionContext.InvocationId },
                { "log", log },
                { "bindings", bindings },
                { "bind", bind }
            };

            var invocationContext = new InvocationContext(bindingArguments, context);

            //// This is the input value that we will use to extract binding data.
            //// Since binding data extraction is based on JSON parsing, in the
            //// various conversions below, we set this to the appropriate JSON
            //// string when possible.
            //// TODO: (INVOKERWORK) Need to get binding data working
            //// object bindDataInput = input;

            // TODO: (INVOKERWORK) Need to get this working
            // context["bindingData"] = GetBindingData(bindDataInput, binder);
            invocationContext.ExecutionContext["bindingData"] = new Dictionary<string, string>();

            await ProcessInputParametersAsync(invocationContext, bindings);

            return context;
        }

        /// <summary>
        /// Performs required static initialization in the Edge context.
        /// </summary>
        private static void Initialize()
        {
            var handle = (Func<object, Task<object>>)(err =>
            {
                if (UnhandledException != null)
                {
                    // raise the event to allow subscribers to handle
                    var ex = new InvalidOperationException((string)err);
                    UnhandledException(null, new UnhandledExceptionEventArgs(ex, true));

                    // Ensure that we allow the unhandled exception to kill the process.
                    // unhandled Node global exceptions should never be swallowed.
                    throw ex;
                }
                return Task.FromResult<object>(null);
            });
            var context = new Dictionary<string, object>()
            {
                { "handleUncaughtException", handle }
            };

            GlobalInitializationFunc(context).GetAwaiter().GetResult();
        }

        private static string ReadResourceString(string fileName)
        {
            string resourcePath = string.Format("Microsoft.Azure.WebJobs.Script.Description.Node.Script.{0}", fileName);
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourcePath)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}