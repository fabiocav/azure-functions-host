// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Tests.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests
{
    public class DotNetEndToEndTests : EndToEndTestsBase<DotNetEndToEndTests.TestFixture>
    {
        public DotNetEndToEndTests(TestFixture fixture) : base(fixture)
        {
        }

        [Fact]
        public async Task Invoking_DotNetFunction()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://functions/myfunc");
            Dictionary<string, object> arguments = new Dictionary<string, object>()
            {
                { "req", request }
            };

            await Fixture.Host.CallAsync("DotNetFunction", arguments);

            HttpResponseMessage response = (HttpResponseMessage)request.Properties[ScriptConstants.AzureFunctionsHttpResponseKey];

            Assert.Equal("Hello from .NET", await response.Content.ReadAsStringAsync());
        }

        public class TestFixture : EndToEndTestFixture
        {
            private const string ScriptRoot = @"TestScripts\DotNet";
            private static readonly string FunctionPath;

            static TestFixture()
            {
                FunctionPath = Path.Combine(ScriptRoot, "DotNetFunction");
                CreateFunctionAssembly();
            }

            public TestFixture() : base(ScriptRoot, "dotnet")
            {
            }

            public override void Dispose()
            {
                base.Dispose();

                File.Delete(Path.Combine(FunctionPath, "DotNetFunctionAssembly.dll"));
                File.Delete(Path.Combine(FunctionPath, "function.json"));
            }

            private static void CreateFunctionAssembly()
            {
                File.Delete(Path.Combine(FunctionPath, "DotNetFunctionAssembly.dll"));
                File.Delete(Path.Combine(FunctionPath, "function.json"));

                string primaryReferenceSource = @"
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace TestFunction
{
    public class Function
    {
        public static Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
        {
            log.Info(""Test"");

            var res = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(""Hello from .NET"")
            };

            return Task.FromResult(res);
        }
    }
}
";

                var primarySyntaxTree = CSharpSyntaxTree.ParseText(primaryReferenceSource);
                Compilation primaryCompilation = CSharpCompilation.Create("DotNetFunctionAssembly", new[] { primarySyntaxTree })
                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                    .WithReferences(MetadataReference.CreateFromFile(typeof(TraceWriter).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HttpRequestMessage).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(HttpStatusCode).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

                var result = primaryCompilation.Emit(Path.Combine(FunctionPath, "DotNetFunctionAssembly.dll"));

                // Create function metadata
                File.WriteAllText(Path.Combine(FunctionPath, "function.json"), Resources.DotNetFunctionJson);
            }
        }
    }
}
