// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class BashEndToEndTests : EndToEndTestsBase<BashEndToEndTests.BashTestFixture>
    {
        public BashEndToEndTests(BashTestFixture fixture)
            : base(fixture)
        {
        }

        public class BashTestFixture : EndToEndTestFixture
        {
            public BashTestFixture() : base(@"TestScripts\Bash")
            {
            }
        }
    }
}
