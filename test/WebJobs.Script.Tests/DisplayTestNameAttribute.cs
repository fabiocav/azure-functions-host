// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class DisplayTestNameAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine("Running test: '{0}'", methodUnderTest.Name);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Console.WriteLine("Completed test: '{0}'", methodUnderTest.Name);
        }
    }
}
