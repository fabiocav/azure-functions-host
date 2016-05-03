// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Binding;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    internal class NodeFunctionDescriptorProvider : FunctionDescriptorProvider
    {
        public NodeFunctionDescriptorProvider(ScriptHost host, ScriptHostConfiguration config)
            : base(host, config)
        {
        }

        public override bool TryCreate(FunctionMetadata functionMetadata, out FunctionDescriptor functionDescriptor)
        {
            if (functionMetadata == null)
            {
                throw new ArgumentNullException("functionMetadata");
            }

            functionDescriptor = null;

            if (functionMetadata.ScriptType != ScriptType.Javascript)
            {
                return false;
            }

            return base.TryCreate(functionMetadata, out functionDescriptor);
        }

        protected override IFunctionInvoker CreateFunctionInvoker(string scriptFilePath, BindingMetadata triggerMetadata, FunctionMetadata functionMetadata, Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings)
        {
            return new NodeFunctionInvoker(Host, triggerMetadata, functionMetadata, inputBindings, outputBindings);
        }

        protected override Collection<ParameterDescriptor> GetFunctionParameters(IFunctionInvoker functionInvoker, FunctionMetadata functionMetadata, BindingMetadata triggerMetadata, Collection<CustomAttributeBuilder> methodAttributes, Collection<FunctionBinding> inputBindings, Collection<FunctionBinding> outputBindings)
        {
            ApplyMethodLevelAttributes(functionMetadata, triggerMetadata, methodAttributes);

            var parameterDescriptors = new Collection<ParameterDescriptor>();
            // Add a TraceWriter for logging
            parameterDescriptors.Add(new ParameterDescriptor("log", typeof(TraceWriter)));

            // Add ExecutionContext to provide access to InvocationId, etc.
            parameterDescriptors.Add(new ParameterDescriptor("context", typeof(ExecutionContext)));

            var triggerParameter = CreateTriggerParameter(triggerMetadata);
            parameterDescriptors.Add(triggerParameter);

            IEnumerable<FunctionBinding> bindings = inputBindings
                .Where(b => !b.Metadata.IsTrigger)
                .Union(outputBindings.Where(b => b.Metadata.Type != BindingType.Http));

            foreach (var binding in bindings)
            {
                var descriptor = new ParameterDescriptor(binding.Metadata.Name, binding.DefaultType);
                Collection<CustomAttributeBuilder> customAttributes = binding.GetCustomAttributes(binding.DefaultType);
                if (customAttributes != null)
                {
                    foreach (var customAttribute in customAttributes)
                    {
                        descriptor.CustomAttributes.Add(customAttribute);
                    }
                }

                parameterDescriptors.Add(descriptor);
            }

            return parameterDescriptors;
        }
    }
}
