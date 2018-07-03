// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Xunit;

namespace Microsoft.Azure.WebJobs.Script.Tests.Description.DotNet.CSharp
{
    public class CachedCSharpCompilationTests
    {
        [Fact]
        public void EmitAsync_WithSameCode_UsesCachedCompilation()
        {
            string code = @"
#r ""System.Runtime.dll""
public void Run(){
}";

            Assert.True(IsEmitInvoked(nameof(EmitAsync_WithSameCode_UsesCachedCompilation), code));
            Assert.False(IsEmitInvoked(nameof(EmitAsync_WithSameCode_UsesCachedCompilation), code));
        }

        [Fact]
        public void EmitAsync_WithDifferentCode_InvokesCompilation()
        {
            string code = @"
#r ""System.Runtime.dll""
public void Run(){
}";

            string code2 = @"
#r ""System.Runtime.dll""
public void Run(){
 // Some comment here
}";
            Assert.True(IsEmitInvoked(nameof(EmitAsync_WithDifferentCode_InvokesCompilation), code));
            Assert.True(IsEmitInvoked(nameof(EmitAsync_WithDifferentCode_InvokesCompilation), code2));
        }

        [Fact]
        public void EmitAsync_WithSameCode_AndDifferentReferences_UsesCachedCompilation()
        {
            string code = @"
#r ""System.Runtime.dll""
public void Run(){
}";
            Assert.True(IsEmitInvoked(nameof(EmitAsync_WithSameCode_AndDifferentReferences_UsesCachedCompilation), code));

            bool emitInvoked = IsEmitInvoked(nameof(EmitAsync_WithSameCode_AndDifferentReferences_UsesCachedCompilation), code,
                MetadataReference.CreateFromFile(typeof(CachedCSharpCompilationTests).Assembly.Location));

            Assert.True(emitInvoked, "Invoke with updated references failed");
        }

        private bool IsEmitInvoked(string functionName, string code, params MetadataReference[] additionalReferences)
        {
            FunctionMetadata testMetadata = CreateTestMetadata(functionName);
            TestCompilation compilation = CreateTestCompilation(code, assembly: null, additionalReferences);

            var cachedCompilation = new CachedCSharpCompilation(compilation, testMetadata);
            cachedCompilation.EmitAsync(CancellationToken.None);

            return compilation.EmitCalled;
        }

        private TestCompilation CreateTestCompilation(string code, Assembly assembly = null, params MetadataReference[] additionalReferences)
        {
            assembly = assembly ?? typeof(CachedCSharpCompilationTests).Assembly;

            Script<object> script = CSharpScript.Create(code);
            Compilation compilation = script.GetCompilation();
            if (additionalReferences?.Length > 0)
            {
                compilation = compilation.WithReferences(additionalReferences);
            }

            return new TestCompilation(assembly, compilation);
        }

        private FunctionMetadata CreateTestMetadata(string functionName)
        {
            return new FunctionMetadata
            {
                Name = functionName,
            };
        }

        private class TestCompilation : ICSharpCompilation
        {
            private readonly Assembly _assembly;
            private readonly Compilation _compilation;

            public TestCompilation(Assembly assembly, Compilation compilation)
            {
                _assembly = assembly;
                _compilation = compilation;
            }

            public Compilation Compilation => _compilation;

            public bool EmitCalled { get; private set; }

            public Task<Assembly> EmitAsync(CancellationToken cancellationToken)
            {
                EmitCalled = true;
                return Task.FromResult(_assembly);
            }

            public ImmutableArray<Diagnostic> GetDiagnostics()
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            public FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver)
            {
                throw new NotImplementedException();
            }

            Task<object> ICompilation.EmitAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
