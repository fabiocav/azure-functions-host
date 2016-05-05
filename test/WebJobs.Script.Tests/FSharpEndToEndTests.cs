// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class FSharpEndToEndTests : EndToEndTestsBase<FSharpEndToEndTests.FSharpTestFixture>
    {
        public FSharpEndToEndTests(FSharpTestFixture fixture)
            : base(fixture)
        {
        }

        public class FSharpTestFixture : EndToEndTestFixture
        {
            public FSharpTestFixture() : base(@"TestScripts\FSharp")
            {
            }
        }
    }
}
