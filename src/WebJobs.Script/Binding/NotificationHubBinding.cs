// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    internal class NotificationHubBinding : FunctionBinding
    {
        public NotificationHubBinding(ScriptHostConfiguration config, NotificationHubBindingMetadata metadata, FileAccess access) :
            base(config, metadata, access)
        {
            TagExpression = metadata.TagExpression;
            Platform = metadata.Platform;
            ConnectionString = metadata.Connection;
            HubName = metadata.HubName;
        }

        public string TagExpression { get; private set; }

        public NotificationPlatform Platform { get; private set; }

        public string ConnectionString { get; private set; }

        public string HubName { get; private set; }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            Type attributeType = typeof(NotificationHubAttribute);
            PropertyInfo[] props = new[]
            {
                attributeType.GetProperty("TagExpression"),
                attributeType.GetProperty("Platform"),
                attributeType.GetProperty("ConnectionString"),
                attributeType.GetProperty("HubName")
            };

            object[] propValues = new object[]
            {
                TagExpression,
                Platform,
                ConnectionString,
                HubName
            };

            ConstructorInfo constructor = attributeType.GetConstructor(System.Type.EmptyTypes);
            return new Collection<CustomAttributeBuilder>()
            {
                new CustomAttributeBuilder(constructor, new object[] { }, props, propValues)
            };
        }
    }
}
