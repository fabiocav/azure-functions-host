// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.WindowsAzure.Storage.Table;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    [CLSCompliant(false)]
    public class TableBinding : FunctionBinding
    {
        public TableBinding(ScriptHostConfiguration config, TableBindingMetadata metadata, FileAccess access) 
            : base(config, metadata, access)
        {
            if (string.IsNullOrEmpty(metadata.TableName))
            {
                throw new ArgumentException("The table name cannot be null or empty.");
            }

            TableName = metadata.TableName;
            PartitionKey = metadata.PartitionKey;
            RowKey = metadata.RowKey;
            Filter = metadata.Filter;

            Take = metadata.Take ?? 50;
        }

        public string TableName { get; private set; }

        public string PartitionKey { get; private set; }

        public string RowKey { get; private set; }

        public int Take { get; private set; }

        public string Filter { get; private set; }

        public override Type GetArgumentType()
        {
            if (Access == FileAccess.Write)
            {
                return typeof(IAsyncCollector<DynamicTableEntity>);
            }
            else
            {
                return typeof(CloudTable);
            }
        }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            Collection<CustomAttributeBuilder> attributes = new Collection<CustomAttributeBuilder>();

            Type[] constructorTypes = null;
            object[] constructorArguments = null;
            if (Access == FileAccess.Write)
            {
                constructorTypes = new Type[] { typeof(string) };
                constructorArguments = new object[] { TableName };
            }
            else
            {
                if (!string.IsNullOrEmpty(PartitionKey) && !string.IsNullOrEmpty(RowKey))
                {
                    constructorTypes = new Type[] { typeof(string), typeof(string), typeof(string) };
                    constructorArguments = new object[] { TableName, PartitionKey, RowKey };
                }
                else
                {
                    constructorTypes = new Type[] { typeof(string) };
                    constructorArguments = new object[] { TableName };
                }
            }

            attributes.Add(new CustomAttributeBuilder(typeof(TableAttribute).GetConstructor(constructorTypes), constructorArguments));

            if (!string.IsNullOrEmpty(Metadata.Connection))
            {
                AddStorageAccountAttribute(attributes, Metadata.Connection);
            }

            return attributes;
        }    
    }
}
