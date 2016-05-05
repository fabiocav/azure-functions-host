// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class PythonEndToEndTests : EndToEndTestsBase<PythonEndToEndTests.PythonTestFixture>
    {
        public PythonEndToEndTests(PythonTestFixture fixture)
            : base(fixture)
        {
        }

        public class PythonTestFixture : EndToEndTestFixture
        {
            public PythonTestFixture() : base(@"TestScripts\Python")
            {
            }
        }
    }
}
