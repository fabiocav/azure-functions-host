// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class BlobBinding : FunctionBinding
    {
        public BlobBinding(ScriptHostConfiguration config, BlobBindingMetadata metadata, FileAccess access) : base(config, metadata, access)
        {
            if (string.IsNullOrEmpty(metadata.Path))
            {
                throw new ArgumentException("The blob path cannot be null or empty.");
            }

            Path = metadata.Path;
        }

        public string Path { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            Collection<CustomAttributeBuilder> attributes = new Collection<CustomAttributeBuilder>();

            FileAccess access = GetAttributeAccess(parameterType);

            var constructorTypes = new Type[] { typeof(string), typeof(FileAccess) };
            var constructorArguments = new object[] { Path, access };
            var attribute = new CustomAttributeBuilder(typeof(BlobAttribute).GetConstructor(constructorTypes), constructorArguments);

            attributes.Add(attribute);

            if (!string.IsNullOrEmpty(Metadata.Connection))
            {
                AddStorageAccountAttribute(attributes, Metadata.Connection);
            }

            return attributes;
        }

        private FileAccess GetAttributeAccess(Type parameterType)
        {
            // The types bellow only support Read/Write access.
            // When using them we ignore the acces and always assume ReadWrite
            if (parameterType == typeof(ICloudBlob) || parameterType == typeof(CloudBlockBlob) ||
               parameterType == typeof(CloudPageBlob) || parameterType == typeof(CloudBlobDirectory))
            {
                return FileAccess.ReadWrite;
            }

            return Access;
        }
    }
}
