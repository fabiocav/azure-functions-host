// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class PhpEndToEndTests : EndToEndTestsBase<PhpEndToEndTests.PhpTestFixture>
    {
        public PhpEndToEndTests(PhpTestFixture fixture)
            : base(fixture)
        {
        }

        public class PhpTestFixture : EndToEndTestFixture
        {
            public PhpTestFixture() : base(@"TestScripts\Php")
            {
            }
        }
    }
}
