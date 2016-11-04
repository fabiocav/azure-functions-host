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
        private Func<DotNetCompilation> _compilationFactory;
        private DotNetCompilation _compilation;
        private readonly FunctionMetadata _functionMetadata;
        private const int DefaultBufferSize = 81920;

        public CachedCompilation(FunctionMetadata functionMetadata, Func<DotNetCompilation> compilation)
        {
            _functionMetadata = functionMetadata;
            _compilationFactory = compilation;
        }

        private string CachePath
        {
            get
            {
                string result = null;
                string home = ScriptSettingsManager.Instance.GetSetting(EnvironmentSettingNames.AzureWebsiteHomePath);
                if (!string.IsNullOrEmpty(home))
                {
                    result = Path.Combine(home, $"data\\Functions\\compilation_cache", _functionMetadata.Name);
                }
                else
                {
                    string userProfile = Environment.ExpandEnvironmentVariables("%userprofile%");
                    result = Path.Combine(userProfile, ".functions\\compilation_cache", _functionMetadata.Name);
                }

                return result;
            }
        }

        private DotNetCompilation Compilation => _compilation ?? (_compilation = _compilationFactory());

        private string CachedAssemblyPath => Path.Combine(CachePath, $"{_functionMetadata.Name}.dll");

        private string CachedSymbolsPath => Path.Combine(CachePath, $"{_functionMetadata.Name}.pdb");

        public override void Emit(Stream assemblyStream, Stream pdbStream, CancellationToken cancellationToken)
        {
            if (File.Exists(CachedAssemblyPath))
            {
                Task assemblyBytesReadTask = ReadFileAsync(CachedAssemblyPath, assemblyStream);

                Task symbolsBytesReadTask = Task.CompletedTask;
                if (File.Exists(CachedAssemblyPath))
                {
                    symbolsBytesReadTask = ReadFileAsync(CachedSymbolsPath, pdbStream);
                }

                Task.WaitAll(new Task[] { assemblyBytesReadTask, symbolsBytesReadTask }, cancellationToken);
            }
            else
            {
                var compilation = _compilationFactory();
                compilation.Emit(assemblyStream, pdbStream, cancellationToken);

                CacheCompilationArtifacts(assemblyStream, pdbStream, cancellationToken);
            }
        }

        private void CacheCompilationArtifacts(Stream assemblyStream, Stream pdbStream, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(CachePath);

            // Write asynchronously, no need to wait for this
            WriteFileAsync(CachedAssemblyPath, assemblyStream, cancellationToken)
                .ContinueWith(t => { /*log failure*/ }, TaskContinuationOptions.OnlyOnFaulted);

            if (pdbStream != null && pdbStream.Length > 0)
            {
                WriteFileAsync(CachedSymbolsPath, pdbStream, cancellationToken)
                    .ContinueWith(t => { /*log failure*/ }, TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        public override ImmutableArray<Diagnostic> GetDiagnostics() => Compilation.GetDiagnostics();

        public override FunctionSignature GetEntryPointSignature(IFunctionEntryPointResolver entryPointResolver) => Compilation.GetEntryPointSignature(entryPointResolver);

        private async Task ReadFileAsync(string path, Stream destinationStream)
        {
            using (FileStream fileStream = File.Open(path, FileMode.Open))
            {
                await fileStream.CopyToAsync(destinationStream);
            }
        }

        private async Task WriteFileAsync(string path, Stream source, CancellationToken cancellationToken)
        {
            source.Seek(0, SeekOrigin.Begin);
            using (FileStream fileStream = File.Open(path, FileMode.CreateNew))
            {
                await source.CopyToAsync(fileStream, DefaultBufferSize, cancellationToken);
            }
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
