using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.CodeAnalysis;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    [CLSCompliant(false)]
    public class CachedCompilation : DotNetCompilation
    {
        private ICompilation _compilation;
        private readonly FunctionMetadata _functionMetadata;

        public CachedCompilation(FunctionMetadata functionMetadata, ICompilation compilation)
        {
            _functionMetadata = functionMetadata;
            _compilation = compilation;
        }

        private string CachePath
        {
            get
            {
                string result = null;
                string home = ScriptSettingsManager.Instance.GetSetting(EnvironmentSettingNames.AzureWebsiteHomePath);
                if (!string.IsNullOrEmpty(home))
                {
                    result = Path.Combine(home, $"data\\Functions\\compilation_cache\\");
                }
                else
                {
                    string userProfile = Environment.ExpandEnvironmentVariables("%userprofile%");
                    result = Path.Combine(userProfile, ".nuget\\packages");
                }
            }
        }
        public override void Emit(Stream assemblyStream, Stream pdbStream, CancellationToken cancellationToken)
        {
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics()
        {
            throw new NotImplementedException();
        }

        public override FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver)
        {
            throw new NotImplementedException();
        }
    }

    [CLSCompliant(false)]
    public abstract class DotNetCompilation : ICompilation
    {
        public virtual Assembly EmitAndLoad(CancellationToken cancellationToken)
        {
            using (var assemblyStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                Emit(assemblyStream, pdbStream, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                return Assembly.Load(assemblyStream.GetBuffer(), pdbStream?.GetBuffer());
            }
        }

        public abstract void Emit(Stream assemblyStream, Stream pdbStream, CancellationToken cancellationToken);

        public abstract ImmutableArray<Diagnostic> GetDiagnostics();

        public abstract FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver);
    }
}
