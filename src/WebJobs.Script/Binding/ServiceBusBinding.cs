// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class ServiceBusBinding : FunctionBinding
    {
        public ServiceBusBinding(ScriptHostConfiguration config, ServiceBusBindingMetadata metadata, FileAccess access) : 
            base(config, metadata, access)
        {
            string queueOrTopicName = metadata.QueueName ?? metadata.TopicName;
            if (string.IsNullOrEmpty(queueOrTopicName))
            {
                throw new ArgumentException("A valid queue or topic name must be specified.");
            }

            QueueOrTopicName = queueOrTopicName;
        }

        public string QueueOrTopicName { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            var constructorTypes = new Type[] { typeof(string) };
            var constructorArguments = new object[] { QueueOrTopicName };

            var attributes = new Collection<CustomAttributeBuilder>
            {
                new CustomAttributeBuilder(typeof(ServiceBusAttribute).GetConstructor(constructorTypes), constructorArguments)
            };

            if (!string.IsNullOrEmpty(Metadata.Connection))
            {
                AddServiceBusAccountAttribute(attributes, Metadata.Connection);
            }

            return attributes;
        }

        internal static void AddServiceBusAccountAttribute(Collection<CustomAttributeBuilder> attributes, string connection)
        {
            var constructorTypes = new Type[] { typeof(string) };
            var constructorArguments = new object[] { connection };
            var attribute = new CustomAttributeBuilder(typeof(ServiceBusAccountAttribute).GetConstructor(constructorTypes), constructorArguments);
            attributes.Add(attribute);
        }
    }
}
