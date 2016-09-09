// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class SecretManager : IDisposable
    {
        public const string MasterKeyName = "master";
        public const string HostFunctionKeyName = "function";
        public const string DefaultFunctionKeyName = "default";

        private readonly string _secretsPath;
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _secretsMap = new ConcurrentDictionary<string, Dictionary<string, string>>();
        private readonly ScriptSecretReader _scriptSecretReader = new ScriptSecretReader();
        private readonly IKeyValueConverterFactory _keyValueConverterFactory;
        private readonly FileSystemWatcher _fileWatcher;
        private HostSecretsInfo _hostSecrets;

        // for testing
        public SecretManager()
        {
        }

        public SecretManager(string secretsPath)
            : this(secretsPath, new DefaultKeyValueConverterFactory())
        {
        }

        public SecretManager(string secretsPath, IKeyValueConverterFactory keyValueConverterFactory)
        {
            _secretsPath = secretsPath;
            _keyValueConverterFactory = keyValueConverterFactory;

            Directory.CreateDirectory(_secretsPath);

            _fileWatcher = new FileSystemWatcher(_secretsPath, "*.json")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Deleted += OnChanged;
            _fileWatcher.Renamed += OnChanged;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileWatcher?.Dispose();
            }
        }

        public virtual HostSecretsInfo GetHostSecrets()
        {
            if (_hostSecrets == null)
            {
                string secretFilePath = Path.Combine(_secretsPath, ScriptConstants.HostMetadataFileName);
                var hostSecrets = new HostSecrets();

                if (File.Exists(secretFilePath))
                {
                    // load the secrets file
                    string secretsJson = File.ReadAllText(secretFilePath);
                    hostSecrets = _scriptSecretReader.ReadHostSecrets(secretsJson);
                }
                else
                {
                    // initialize with new secrets and save it
                    hostSecrets = new HostSecrets
                    {
                        MasterKey = GenerateSecret(MasterKeyName),
                        FunctionKeys = new List<Key>
                        {
                            GenerateSecret(HostFunctionKeyName)
                        }
                    };

                    string secretContent = _scriptSecretReader.WriteHostSecrets(hostSecrets);
                    File.WriteAllText(secretFilePath, secretContent);
                }

                _hostSecrets = new HostSecretsInfo
                {
                    MasterKey = ReadSecretValue(hostSecrets.MasterKey),
                    FunctionKeys = hostSecrets.FunctionKeys.ToDictionary(s => s.Name, s => ReadSecretValue(s))
                };
            }

            return _hostSecrets;
        }

        public virtual Dictionary<string, string> GetFunctionSecrets(string functionName)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentNullException("functionName");
            }

            functionName = functionName.ToLowerInvariant();

            return _secretsMap.GetOrAdd(functionName, n =>
            {
                IList<Key> secrets;
                string secretFileName = string.Format(CultureInfo.InvariantCulture, "{0}.json", functionName);
                string secretFilePath = Path.Combine(_secretsPath, secretFileName);
                if (File.Exists(secretFilePath))
                {
                    // load the secrets file
                    string secretsJson = File.ReadAllText(secretFilePath);
                    secrets = _scriptSecretReader.ReadFunctionSecrets(secretsJson);
                }
                else
                {
                    // initialize with new list with a default secret and save it
                    secrets = new List<Key>
                    {
                        GenerateSecret(DefaultFunctionKeyName)
                    };

                    string secretsContent = _scriptSecretReader.WriteFunctionSecrets(secrets);
                    File.WriteAllText(secretFilePath, secretsContent);
                }

                return secrets.ToDictionary(s => s.Name, s => ReadSecretValue(s));
            });
        }

        private string ReadSecretValue(Key key)
        {
            IKeyValueConverter converter = _keyValueConverterFactory.GetValueConverter(key, KeyConversionAction.Read);
            return converter.ReadKeyValue(key);
        }

        private Key WriteSecretValue(Key key)
        {
            IKeyValueConverter converter = _keyValueConverterFactory.GetValueConverter(key, KeyConversionAction.Write);
            return converter.WriteKeyValue(key);
        }

        /// <summary>
        /// Iterate through all function secret files and remove any that don't correspond
        /// to a function.
        /// </summary>
        /// <param name="rootScriptPath">The root function directory.</param>
        /// <param name="traceWriter">The TraceWriter to log to.</param>
        public void PurgeOldFiles(string rootScriptPath, TraceWriter traceWriter)
        {
            try
            {
                if (!Directory.Exists(rootScriptPath))
                {
                    return;
                }

                // Create a lookup of all potential functions (whether they're valid or not)
                // It is important that we determine functions based on the presence of a folder,
                // not whether we've identified a valid function from that folder. This ensures
                // that we don't delete logs/secrets for functions that transition into/out of
                // invalid unparsable states.
                var functionLookup = Directory.EnumerateDirectories(rootScriptPath).ToLookup(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase);

                var secretsDirectory = new DirectoryInfo(_secretsPath);
                if (!Directory.Exists(_secretsPath))
                {
                    return;
                }

                foreach (var secretFile in secretsDirectory.GetFiles("*.json"))
                {
                    if (string.Compare(secretFile.Name, ScriptConstants.HostMetadataFileName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        // the secrets directory contains the host secrets file in addition
                        // to function secret files
                        continue;
                    }

                    string fileName = Path.GetFileNameWithoutExtension(secretFile.Name);
                    if (!functionLookup.Contains(fileName))
                    {
                        try
                        {
                            secretFile.Delete();
                        }
                        catch
                        {
                            // Purge is best effort
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Purge is best effort
                traceWriter.Error("An error occurred while purging secret files", ex);
            }
        }

        private Key GenerateSecret(string name = null)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[40];
                rng.GetBytes(data);
                string secret = Convert.ToBase64String(data);

                // Replace pluses as they are problematic as URL values
                secret = secret.Replace('+', 'a');

                var key = new Key
                {
                    Name = name,
                    Value = secret
                };

                return WriteSecretValue(key);
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            // clear the cached secrets if they exist
            // they'll be reloaded on demand next time
            if (string.Compare(Path.GetFileName(e.FullPath), ScriptConstants.HostMetadataFileName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                _hostSecrets = null;
            }
            else
            {
                Dictionary<string, string> secrets;
                string name = Path.GetFileNameWithoutExtension(e.FullPath).ToLowerInvariant();
                _secretsMap.TryRemove(name, out secrets);
            }
        }
    }
}