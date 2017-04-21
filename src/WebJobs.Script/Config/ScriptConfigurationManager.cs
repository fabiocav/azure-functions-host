// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Config
{
    public static class ScriptConfigurationManager
    {
        private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(5);

        public static ScriptHostConfiguration LoadHostConfiguration(ScriptSettingsManager settingsManager,  ScriptHostEnvironmentSettings environmentSettings)
        {
            var configuration = new ScriptHostConfiguration
            {
                RootScriptPath = environmentSettings.ScriptPath,
                RootLogPath = environmentSettings.LogPath,
                FileLoggingMode = FileLoggingMode.DebugOnly,
                TraceWriter = environmentSettings.TraceWriter
            };

            // read host.json and apply to JobHostConfiguration
            string hostConfigFilePath = Path.Combine(configuration.RootScriptPath, ScriptConstants.HostMetadataFileName);

            // If it doesn't exist, create an empty JSON file
            if (!File.Exists(hostConfigFilePath))
            {
                File.WriteAllText(hostConfigFilePath, "{}");
            }

            string json = File.ReadAllText(hostConfigFilePath);
            JObject hostConfig;
            try
            {
                hostConfig = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new FormatException(string.Format("Unable to parse {0} file.", ScriptConstants.HostMetadataFileName), ex);
            }

            configuration.HostConfig.HostConfigMetadata = hostConfig;

            ApplyConfiguration(hostConfig, configuration, settingsManager);

            return configuration;
        }

        internal static void ApplyConfiguration(JObject config, ScriptHostConfiguration scriptConfig, ScriptSettingsManager settingsManager)
        {
            JobHostConfiguration hostConfig = scriptConfig.HostConfig;

            JArray functions = (JArray)config["functions"];
            if (functions != null && functions.Count > 0)
            {
                scriptConfig.Functions = new Collection<string>();
                foreach (var function in functions)
                {
                    scriptConfig.Functions.Add((string)function);
                }
            }

            // We may already have a host id, but the one from the JSON takes precedence
            JToken hostId = (JToken)config["id"];
            if (hostId != null)
            {
                hostConfig.HostId = (string)hostId;
            }

            if (string.IsNullOrEmpty(scriptConfig.HostConfig.HostId))
            {
                scriptConfig.HostConfig.HostId = Utility.GetDefaultHostId(settingsManager, scriptConfig);
            }

            JToken fileWatchingEnabled = (JToken)config["fileWatchingEnabled"];
            if (fileWatchingEnabled != null && fileWatchingEnabled.Type == JTokenType.Boolean)
            {
                scriptConfig.FileWatchingEnabled = (bool)fileWatchingEnabled;
            }

            // Configure the set of watched directories, adding the standard built in
            // set to any the user may have specified
            if (scriptConfig.WatchDirectories == null)
            {
                scriptConfig.WatchDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            scriptConfig.WatchDirectories.Add("node_modules");
            JToken watchDirectories = config["watchDirectories"];
            if (watchDirectories != null && watchDirectories.Type == JTokenType.Array)
            {
                foreach (JToken directory in watchDirectories.Where(p => p.Type == JTokenType.String))
                {
                    scriptConfig.WatchDirectories.Add((string)directory);
                }
            }

            ApplySingletonConfiguration(config, hostConfig);

            ApplyTracingConfiguration(config, scriptConfig, hostConfig);

            // Function timeout
            JToken value = null;
            if (config.TryGetValue("functionTimeout", out value))
            {
                TimeSpan requestedTimeout = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);

                // Only apply limits if this is Dynamic.
                if (ScriptSettingsManager.Instance.IsDynamicSku && (requestedTimeout < MinTimeout || requestedTimeout > MaxTimeout))
                {
                    string message = $"{nameof(scriptConfig.FunctionTimeout)} must be between {MinTimeout} and {MaxTimeout}.";
                    throw new ArgumentException(message);
                }

                scriptConfig.FunctionTimeout = requestedTimeout;
            }
            else if (ScriptSettingsManager.Instance.IsDynamicSku)
            {
                // Apply a default if this is running on Dynamic.
                scriptConfig.FunctionTimeout = MaxTimeout;
            }

            ApplySwaggerConfiguration(config, scriptConfig);
        }

        private static void ApplySwaggerConfiguration(JObject config, ScriptHostConfiguration scriptConfig)
        {
            // apply swagger configuration
            scriptConfig.SwaggerEnabled = false;

            var configSection = (JObject)config["swagger"];
            JToken swaggerEnabled;

            if (configSection != null &&
                configSection.TryGetValue("enabled", out swaggerEnabled) &&
                swaggerEnabled.Type == JTokenType.Boolean)
            {
                scriptConfig.SwaggerEnabled = (bool)swaggerEnabled;
            }
        }

        private static JObject ApplyTracingConfiguration(JObject config, ScriptHostConfiguration scriptConfig, JobHostConfiguration hostConfig)
        {
            // Apply Tracing/Logging configuration
            var configSection = (JObject)config["tracing"];
            JToken value = null;
            if (configSection != null)
            {
                if (configSection.TryGetValue("consoleLevel", out value))
                {
                    TraceLevel consoleLevel;
                    if (Enum.TryParse<TraceLevel>((string)value, true, out consoleLevel))
                    {
                        hostConfig.Tracing.ConsoleLevel = consoleLevel;
                    }
                }

                if (configSection.TryGetValue("fileLoggingMode", out value))
                {
                    FileLoggingMode fileLoggingMode;
                    if (Enum.TryParse<FileLoggingMode>((string)value, true, out fileLoggingMode))
                    {
                        scriptConfig.FileLoggingMode = fileLoggingMode;
                    }
                }
            }

            return configSection;
        }

        private static void ApplySingletonConfiguration(JObject config, JobHostConfiguration hostConfig)
        {
            // Apply Singleton configuration
            JObject configSection = (JObject)config["singleton"];
            JToken value = null;
            if (configSection != null)
            {
                if (configSection.TryGetValue("lockPeriod", out value))
                {
                    hostConfig.Singleton.LockPeriod = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("listenerLockPeriod", out value))
                {
                    hostConfig.Singleton.ListenerLockPeriod = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("listenerLockRecoveryPollingInterval", out value))
                {
                    hostConfig.Singleton.ListenerLockRecoveryPollingInterval = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("lockAcquisitionTimeout", out value))
                {
                    hostConfig.Singleton.LockAcquisitionTimeout = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("lockAcquisitionPollingInterval", out value))
                {
                    hostConfig.Singleton.LockAcquisitionPollingInterval = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
            }
        }
    }
}
