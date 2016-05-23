// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class ApiHubTableBinding : FunctionBinding
    {
        public ApiHubTableBinding(
            ScriptHostConfiguration config, 
            ApiHubTableBindingMetadata metadata, 
            FileAccess access) 
            : base(config, metadata, access)
        {
            Connection = metadata.Connection;
            DataSetName = metadata.DataSetName;
            TableName = metadata.TableName;
            EntityId = metadata.EntityId;
            BindingDirection = metadata.Direction;
        }

        public string Connection { get; }

        public string DataSetName { get; }

        public string TableName { get; }

        public string EntityId { get; }

        public BindingDirection BindingDirection { get; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            var constructorTypes = new[] { typeof(string) };
            var constructor = typeof(ApiHubTableAttribute).GetConstructor(constructorTypes);
            var constructorArguments = new[] { Connection };
            var namedProperties = new[]
            {
                typeof(ApiHubTableAttribute).GetProperty("DataSetName"),
                typeof(ApiHubTableAttribute).GetProperty("TableName"),
                typeof(ApiHubTableAttribute).GetProperty("EntityId")
            };
            var propertyValues = new[]
            {
                DataSetName,
                TableName,
                EntityId
            };

            return new Collection<CustomAttributeBuilder>()
            {
                new CustomAttributeBuilder(
                    constructor,
                    constructorArguments,
                    namedProperties,
                    propertyValues)
            };
        }
    }
}
