// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Bindings.Path;
using Microsoft.Azure.WebJobs.Host.Bindings.Runtime;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.Azure.WebJobs.Script.Extensibility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public abstract class FunctionBinding
    {
        private readonly ScriptHostConfiguration _config;

        protected FunctionBinding(ScriptHostConfiguration config, BindingMetadata metadata, FileAccess access)
        {
            _config = config;
            Access = access;
            Metadata = metadata;
        }

        public BindingMetadata Metadata { get; private set; }

        public FileAccess Access { get; private set; }

        public virtual Type GetArgumentType()
        {
            var dataType = Metadata.DataType ?? DataType.String;

            Type result = null;
            switch (dataType)
            {
                case DataType.String:
                    result = typeof(string);
                    break;
                case DataType.Binary:
                    result = typeof(byte[]);
                    break;
                case DataType.Stream:
                    result = typeof(Stream);
                    break;
                case DataType.Object:
                    result = typeof(JObject);
                    break;
                default:
                    throw new NotSupportedException($"The data type {Metadata.DataType.Value.ToString("G")} is not supported");
            }

            if (Metadata.Direction == BindingDirection.Out)
            {
                result = typeof(IAsyncCollector<>).MakeGenericType(result);
            }

            return result;
        }

        public abstract Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType);

        internal static Collection<FunctionBinding> GetBindings(ScriptHostConfiguration config, IEnumerable<BindingMetadata> bindingMetadatas, FileAccess fileAccess)
        {
            Collection<FunctionBinding> bindings = new Collection<FunctionBinding>();

            if (bindings != null)
            {
                foreach (var bindingMetadata in bindingMetadatas)
                {
                    string type = bindingMetadata.Type.ToLowerInvariant();
                    switch (type)
                    {
                        case "table":
                            TableBindingMetadata tableBindingMetadata = (TableBindingMetadata)bindingMetadata;
                            bindings.Add(new TableBinding(config, tableBindingMetadata, fileAccess));
                            break;
                        case "http":
                            if (fileAccess != FileAccess.Write)
                            {
                                throw new InvalidOperationException("Http binding can only be used for output.");
                            }
                            bindings.Add(new HttpBinding(config, bindingMetadata, FileAccess.Write));
                            break;
                        case "httptrigger":
                            bindings.Add(new HttpBinding(config, bindingMetadata, FileAccess.Read));
                            break;
                        default:
                            FunctionBinding binding = null;
                            if (bindingMetadata.Raw == null)
                            {
                                // TEMP: This conversion is only here to keep unit tests passing
                                bindingMetadata.Raw = JObject.FromObject(bindingMetadata);
                            }
                            if (TryParseFunctionBinding(config, bindingMetadata, out binding))
                            {
                                bindings.Add(binding);
                            }
                            break;
                    }
                }
            }

            return bindings;
        }

        private static bool TryParseFunctionBinding(ScriptHostConfiguration config, BindingMetadata metadata, out FunctionBinding functionBinding)
        {
            functionBinding = null;            

            ScriptBindingContext bindingContext = new ScriptBindingContext(metadata.Raw);
            ScriptBinding scriptBinding = null;
            foreach (var provider in config.BindingProviders)
            {
                if (provider.TryCreate(bindingContext, out scriptBinding))
                {
                    break;
                }
            }

            if (scriptBinding == null)
            {
                return false;
            }

            functionBinding = new ExtensionBinding(config, scriptBinding, metadata);

            return true;
        }

        protected string ResolveBindingTemplate(string value, BindingTemplate bindingTemplate, IReadOnlyDictionary<string, string> bindingData)
        {
            string boundValue = value;

            if (bindingData != null)
            {
                if (bindingTemplate != null)
                {
                    boundValue = bindingTemplate.Bind(bindingData);
                }
            }

            if (!string.IsNullOrEmpty(value))
            {
                boundValue = Resolve(boundValue);
            }

            return boundValue;
        }

        protected string Resolve(string name)
        {
            if (_config.HostConfig.NameResolver == null)
            {
                return name;
            }

            return _config.HostConfig.NameResolver.ResolveWholeString(name);
        }

        internal static void AddStorageAccountAttribute(Collection<CustomAttributeBuilder> attributes, string connection)
        {
            var constructorTypes = new Type[] { typeof(string) };
            var constructorArguments = new object[] { connection };
            var attribute = new CustomAttributeBuilder(typeof(StorageAccountAttribute).GetConstructor(constructorTypes), constructorArguments);
            attributes.Add(attribute);
        }
    }
}
