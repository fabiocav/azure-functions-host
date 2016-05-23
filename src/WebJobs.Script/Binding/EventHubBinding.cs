// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class EventHubBinding : FunctionBinding
    {
        public EventHubBinding(ScriptHostConfiguration config, EventHubBindingMetadata metadata, FileAccess access) : 
            base(config, metadata, access)
        {
            if (string.IsNullOrEmpty(metadata.Path))
            {
                throw new ArgumentException("The event hub path cannot be null or empty.", nameof(metadata));
            }

            EventHubName = metadata.Path;
        }

        public string EventHubName { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            var constructorTypes = new Type[] { typeof(string) };
            var constructorArguments = new object[] { EventHubName };

            return new Collection<CustomAttributeBuilder>
            {
                new CustomAttributeBuilder(typeof(ServiceBus.EventHubAttribute).GetConstructor(constructorTypes), constructorArguments)
            };
        }
    }
}
