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
using Microsoft.Azure.WebJobs.Script.Config;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class SecretManager : IDisposable, ISecretManager
    {
        private readonly string _secretsPath;
        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _secretsMap = new ConcurrentDictionary<string, Dictionary<string, string>>();
        private readonly IKeyValueConverterFactory _keyValueConverterFactory;
        private readonly FileSystemWatcher _fileWatcher;
        private HostSecretsInfo _hostSecrets;
        private readonly ISecretsRepository _repository;

        // for testing
        public SecretManager()
        {
        }

        public SecretManager(ScriptSettingsManager settingsManager, ISecretsRepository repository, bool createHostSecretsIfMissing = false)
            : this(repository, new DefaultKeyValueConverterFactory(settingsManager), createHostSecretsIfMissing)
        {
        }

        public SecretManager(ISecretsRepository repository, IKeyValueConverterFactory keyValueConverterFactory, bool createHostSecretsIfMissing = false)
        {
            _repository = repository;
            _keyValueConverterFactory = keyValueConverterFactory;

            _repository.SecretsChanged += OnSecretsChanged;

            if (createHostSecretsIfMissing)
            {
                // The SecretManager implementation of GetHostSecrets will
                // create a host secret if one is not present.
                GetHostSecrets();
            }
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

        public async virtual Task<HostSecretsInfo> GetHostSecrets()
        {
            if (_hostSecrets == null)
            {
                HostSecrets hostSecrets = await TryLoadSecrets<HostSecrets>();

                if (hostSecrets == null)
                {
                    hostSecrets = GenerateHostSecrets();
                    await PersistSecretsAsync(hostSecrets);
                }

                // Host secrets will be in the original persisted state at this point (e.g. encrypted),
                // so we read the secrets running them through the appropriate readers
                hostSecrets = ReadHostSecrets(hostSecrets);

                // If the persistence state of any of our secrets is stale (e.g. the encryption key has been rotated), update
                // the state and persist the secrets
                if (hostSecrets.HasStaleKeys)
                {
                    await RefreshSecrets(hostSecrets);
                }

                _hostSecrets = new HostSecretsInfo
                {
                    MasterKey = hostSecrets.MasterKey.Value,
                    FunctionKeys = hostSecrets.FunctionKeys.ToDictionary(s => s.Name, s => s.Value)
                };
            }

            return _hostSecrets;
        }

        public async virtual Task<IDictionary<string, string>> GetFunctionSecrets(string functionName, bool merged = false)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentNullException(nameof(functionName));
            }

            functionName = functionName.ToLowerInvariant();
            Dictionary<string, string> functionSecrets;
            _secretsMap.TryGetValue(functionName, out functionSecrets);

            if (functionSecrets == null)
            {
                FunctionSecrets secrets = await TryLoadFunctionSecrets(functionName);
                if (secrets == null)
                {
                    secrets = new FunctionSecrets
                    {
                        Keys = new List<Key>
                        {
                            GenerateKey(ScriptConstants.DefaultFunctionKeyName)
                        }
                    };

                    await PersistSecretsAsync(secrets, functionName);
                }

                // Read all secrets, which will run the keys through the appropriate readers
                secrets.Keys = secrets.Keys.Select(k => _keyValueConverterFactory.ReadKey(k)).ToList();

                if (secrets.HasStaleKeys)
                {
                    await RefreshSecrets(secrets, functionName);
                }

                Dictionary<string, string> result = secrets.Keys.ToDictionary(s => s.Name, s => s.Value);

                functionSecrets = _secretsMap.AddOrUpdate(functionName, result, (n, r) => result);
            }

            if (merged)
            {
                // If merged is true, we combine function specific keys with host level function keys,
                // prioritizing function specific keys
                Dictionary<string, string> hostFunctionSecrets = GetHostSecrets().FunctionKeys;

                functionSecrets = functionSecrets.Union(hostFunctionSecrets.Where(s => !functionSecrets.ContainsKey(s.Key)))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
            }

            return functionSecrets;
        }

        public Task<KeyOperationResult> AddOrUpdateFunctionSecret(string secretName, string secret, string functionName = null)
        {
            ScriptSecretsType secretsType;
            Func<ScriptSecrets> secretsFactory = null;

            if (functionName != null)
            {
                secretsType = ScriptSecretsType.Function;
                secretsFactory = () => new FunctionSecrets(new List<Key>());
            }
            else
            {
                secretsType = ScriptSecretsType.Host;
                secretsFactory = GenerateHostSecrets;
            }

            return AddOrUpdateSecret(secretsType, functionName, secretName, secret, secretsFactory);
        }

        public async Task<KeyOperationResult> SetMasterKey(string value = null)
        {
            HostSecrets secrets = await TryLoadSecrets<HostSecrets>();

            if (secrets == null)
            {
                secrets = GenerateHostSecrets();
            }

            OperationResult result;
            string masterKey;
            if (value == null)
            {
                // Generate a new secret (clear)
                masterKey = GenerateSecret();
                result = OperationResult.Created;
            }
            else
            {
                // Use the provided secret
                masterKey = value;
                result = OperationResult.Updated;
            }

            // Creates a key with the new master key (which will be encrypted, if required)
            secrets.MasterKey = CreateKey(ScriptConstants.DefaultMasterKeyName, masterKey);

            await PersistSecretsAsync(secrets);

            return new KeyOperationResult(masterKey, result);
        }

        public Task<bool> DeleteSecret(string secretName, string functionName = null)
        {
            ScriptSecretsType secretsType = functionName == null
                ? ScriptSecretsType.Host
                : ScriptSecretsType.Function;

            return ModifyFunctionSecret(secretsType, functionName, secretName, (secrets, key) =>
            {
                secrets?.RemoveKey(key);
                return secrets;
            });
        }

        private async Task<KeyOperationResult> AddOrUpdateSecret(ScriptSecretsType secretsType, string functionName, string secretName, string secret, Func<ScriptSecrets> secretsFactory)
        {
            OperationResult result = OperationResult.NotFound;

            secret = secret ?? GenerateSecret();

            await ModifyFunctionSecrets(secretsType, functionName, secrets =>
            {
                Key key = secrets.GetFunctionKey(secretName);

                if (key == null)
                {
                    key = new Key(secretName, secret);
                    secrets.AddKey(key);
                    result = OperationResult.Created;
                }
                else if (secrets.RemoveKey(key))
                {
                    key = CreateKey(secretName, secret);
                    secrets.AddKey(key);

                    result = OperationResult.Updated;
                }

                return secrets;
            }, secretsFactory);

            return new KeyOperationResult(secret, result);
        }

        private async Task<bool> ModifyFunctionSecret(ScriptSecretsType secretsType, string functionName, string secretName, Func<ScriptSecrets, Key, ScriptSecrets> keyChangeHandler, Func<ScriptSecrets> secretFactory = null)
        {
            bool secretFound = false;

            await ModifyFunctionSecrets(secretsType, functionName, secrets =>
            {
                Key key = secrets?.GetFunctionKey(secretName);

                if (key != null)
                {
                    secretFound = true;

                    secrets = keyChangeHandler(secrets, key);
                }

                return secrets;
            }, secretFactory);

            return secretFound;
        }

        private async Task ModifyFunctionSecrets(ScriptSecretsType secretsType, string functionName, Func<ScriptSecrets, ScriptSecrets> changeHandler, Func<ScriptSecrets> secretFactory)
        {
            ScriptSecrets currentSecrets = await TryLoadSecrets(secretsType, functionName);

            if (currentSecrets != null)
            {
                currentSecrets = secretFactory?.Invoke();
            }

            var newSecrets = changeHandler(currentSecrets);

            if (newSecrets != null)
            {
                await PersistSecretsAsync(newSecrets, functionName);
            }
        }

        private Task<FunctionSecrets> TryLoadFunctionSecrets(string functionName)
            => TryLoadSecrets<FunctionSecrets>(functionName);

        private Task<ScriptSecrets> TryLoadSecrets(ScriptSecretsType secretsType, string functionName)
            => TryLoadSecrets(secretsType, functionName, s => ScriptSecretSerializer.DeserializeSecrets(secretsType, s));

        private async Task<T> TryLoadSecrets<T>(string functionName = null) where T : ScriptSecrets
        {
            ScriptSecretsType type = GetSecretsType<T>();

            var result = await TryLoadSecrets(type, functionName, ScriptSecretSerializer.DeserializeSecrets<T>);

            return result as T;
        }

        private async Task<ScriptSecrets> TryLoadSecrets(ScriptSecretsType type, string functionName, Func<string, ScriptSecrets> deserializationHandler)
        {
            string secretsJson = await _repository.ReadAsync(type, functionName).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(secretsJson))
            {
                return deserializationHandler(secretsJson);
            }

            return null;
        }

        private ScriptSecretsType GetSecretsType<T>() where T : ScriptSecrets
        {
            return typeof(HostSecrets).IsAssignableFrom(typeof(T))
                ? ScriptSecretsType.Host
                : ScriptSecretsType.Function;
        }

        private HostSecrets GenerateHostSecrets()
        {
            return new HostSecrets
            {
                MasterKey = GenerateKey(ScriptConstants.DefaultMasterKeyName),
                FunctionKeys = new List<Key>
                {
                    GenerateKey(ScriptConstants.DefaultFunctionKeyName)
                }
            };
        }

        private Task RefreshSecrets<T>(T secrets, string functionName = null) where T : ScriptSecrets
        {
            var refreshedSecrets = secrets.Refresh(_keyValueConverterFactory);

            return PersistSecretsAsync(refreshedSecrets, functionName);
        }

        private Task PersistSecretsAsync<T>(T secrets, string functionName = null) where T : ScriptSecrets
        {
            string secretsContent = ScriptSecretSerializer.SerializeSecrets<T>(secrets);
            return _repository.WriteAsync(secrets.SecretsType, functionName);
        }

        private HostSecrets ReadHostSecrets(HostSecrets hostSecrets)
        {
            return new HostSecrets
            {
                MasterKey = _keyValueConverterFactory.ReadKey(hostSecrets.MasterKey),
                FunctionKeys = hostSecrets.FunctionKeys.Select(k => _keyValueConverterFactory.ReadKey(k)).ToList()
            };
        }

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

        private Key GenerateKey(string name = null)
        {
            string secret = GenerateSecret();

            return CreateKey(name, secret);
        }

        private Key CreateKey(string name, string secret)
        {
            var key = new Key(name, secret);

            return _keyValueConverterFactory.WriteKey(key);
        }

        private static string GenerateSecret()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[40];
                rng.GetBytes(data);
                string secret = Convert.ToBase64String(data);

                // Replace pluses as they are problematic as URL values
                return secret.Replace('+', 'a');
            }
        }

        private void OnSecretsChanged(object sender, SecretsChangedEventArgs e)
        {
            // clear the cached secrets if they exist
            // they'll be reloaded on demand next time
            if (e.Type == ScriptSecretsType.Host)
            {
                _hostSecrets = null;
            }
            else
            {
                Dictionary<string, string> secrets;
                _secretsMap.TryRemove(e.Name, out secrets);
            }
        }
    }
}