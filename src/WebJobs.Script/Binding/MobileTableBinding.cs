// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class MobileTableBinding : FunctionBinding
    {
        public MobileTableBinding(ScriptHostConfiguration config, MobileTableBindingMetadata metadata, FileAccess access) :
            base(config, metadata, access)
        {
            Id = metadata.Id;
            TableName = metadata.TableName;
            MobileAppUri = metadata.Connection;
            ApiKey = metadata.ApiKey;
        }

        public string TableName { get; private set; }

        public string Id { get; private set; }

        public string MobileAppUri { get; private set; }

        public string ApiKey { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            PropertyInfo[] props = new[]
            {
                typeof(MobileTableAttribute).GetProperty("TableName"),
                typeof(MobileTableAttribute).GetProperty("Id"),
                typeof(MobileTableAttribute).GetProperty("MobileAppUri"),
                typeof(MobileTableAttribute).GetProperty("ApiKey"),
            };

            object[] propValues = new[]
            {
                TableName,
                Id,
                MobileAppUri,
                ApiKey
            };

            ConstructorInfo constructor = typeof(MobileTableAttribute).GetConstructor(System.Type.EmptyTypes);

            return new Collection<CustomAttributeBuilder>
            {
                new CustomAttributeBuilder(constructor, new object[] { }, props, propValues)
            };
        }
    }
}
