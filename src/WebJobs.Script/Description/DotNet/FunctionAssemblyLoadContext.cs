// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    /// <summary>
    /// Establishes an assembly load context for a extensions, functions and their dependencies.
    /// </summary>
    public partial class FunctionAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _baseProbingPath;
        private static readonly Lazy<string[]> _runtimeAssemblies = new Lazy<string[]>(GetRuntimeAssemblies);
        private static readonly Lazy<Regex> _unmanagedFileNameRegex = new Lazy<Regex>(CreateUnmanagedFileNameRegex);

        private static Lazy<FunctionAssemblyLoadContext> _defaultContext = new Lazy<FunctionAssemblyLoadContext>(() => new FunctionAssemblyLoadContext(ResolveFunctionBaseProbingPath()), true);

        public FunctionAssemblyLoadContext(string basePath)
        {
            _baseProbingPath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        public static FunctionAssemblyLoadContext Shared => _defaultContext.Value;

        protected virtual Assembly OnResolvingAssembly(AssemblyLoadContext arg1, AssemblyName assemblyName)
        {
            // Log/handle failure
            return null;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (_runtimeAssemblies.Value.Contains(assemblyName.Name))
            {
                return null;
            }

            string path = Path.Combine(_baseProbingPath, assemblyName.Name + ".dll");

            if (File.Exists(path))
            {
                return LoadFromAssemblyPath(path);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // For now, we'll attempt to resolve unmanaged DLLs from the base probing path

            // Directory resolution is be simple for now, we'll assume the base probing
            // path (usually, the bin folder in the function app root), but we need to properly resolve
            // the native module in different platforms:
            // - Windows will append the '.DLL' extension to the name
            // - Linux uses the 'lib' prefix and '.so' suffix. The version may also be appended to the suffix
            // - macOS uses the 'lib' prefix '.dylib'
            // To handle the different scenarios described above, we'll just have a pattern that gives us the ability
            // to match the variations across different platforms. If needed, we can expand this in the future to have
            // logic specific to the platform we're running under.
            string pattern = "";
            Directory.EnumerateFiles(_baseProbingPath, )

            return base.LoadUnmanagedDll(unmanagedDllName);
        }

        protected static string ResolveFunctionBaseProbingPath()
        {
            string basePath = null;
            var settingsManager = ScriptSettingsManager.Instance;
            if (settingsManager.IsAzureEnvironment)
            {
                string home = settingsManager.GetSetting(EnvironmentSettingNames.AzureWebsiteHomePath);
                basePath = Path.Combine(home, "site", "wwwroot");
            }
            else
            {
                basePath = settingsManager.GetSetting(EnvironmentSettingNames.AzureWebJobsScriptRoot) ?? AppContext.BaseDirectory;
            }

            return Path.Combine(basePath, "bin");
        }

        private static string[] GetRuntimeAssemblies()
        {
            string assembliesJson = GetRuntimeAssembliesJson();
            JObject assemblies = JObject.Parse(assembliesJson);

            return assemblies["runtimeAssemblies"].ToObject<string[]>();
        }

        private static string GetRuntimeAssembliesJson()
        {
            var assembly = typeof(FunctionAssemblyLoadContext).Assembly;
            using (Stream resource = assembly.GetManifestResourceStream(assembly.GetName().Name + ".runtimeassemblies.json"))
            using (var reader = new StreamReader(resource))
            {
                return reader.ReadToEnd();
            }
        }

        private static Regex CreateUnmanagedFileNameRegex()
        {
            string pattern = @"(^(?i)testname\.dll$|(?-i)^libtestname\.(so(\.\d+\.\d+\.\d+)?$|dylib$))";
            var regex = new Regex(pattern);
        }
    }
}
