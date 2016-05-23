// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class QueueBinding : FunctionBinding
    {
        public QueueBinding(ScriptHostConfiguration config, QueueBindingMetadata metadata, FileAccess access) : 
            base(config, metadata, access)
        {
            if (string.IsNullOrEmpty(metadata.QueueName))
            {
                throw new ArgumentException("The queue name cannot be null or empty.");
            }

            QueueName = metadata.QueueName;
        }

        public string QueueName { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            Collection<CustomAttributeBuilder> attributes = new Collection<CustomAttributeBuilder>();

            var constructorTypes = new Type[] { typeof(string) };
            var constructorArguments = new object[] { QueueName };

            attributes.Add(new CustomAttributeBuilder(typeof(QueueAttribute).GetConstructor(constructorTypes), constructorArguments));

            if (!string.IsNullOrEmpty(Metadata.Connection))
            {
                AddStorageAccountAttribute(attributes, Metadata.Connection);
            }

            return attributes;
        }
    }
}
