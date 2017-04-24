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
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Config
{
    public static class ScriptConfigurationManager
    {
        private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxTimeout = TimeSpan.FromMinutes(5);

        public static ScriptHostConfiguration BuildHostConfiguration(ScriptSettingsManager settingsManager, ScriptHostEnvironmentSettings environmentSettings)
        {
            var configurationBuilder = new ScriptHostConfiguration.Builder();

            environmentSettings.Build(configurationBuilder);

            var configuration = configurationBuilder.Build();

            // read host.json and apply to JobHostConfiguration
            string hostConfigFilePath = Path.Combine(configuration.RootScriptPath, ScriptConstants.HostMetadataFileName);

            // If it doesn't exist, create an empty JSON file
            if (!File.Exists(hostConfigFilePath))
            {
                File.WriteAllText(hostConfigFilePath, "{}");
            }

            // TODO: || InDebugMode
            if (configuration.HostConfig.IsDevelopment)
            {
                // If we're in debug/development mode, use optimal debug settings
                configuration.HostConfig.UseDevelopmentSettings();
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

            return ApplyConfiguration(hostConfig, environmentSettings, configuration.ToBuilder(), settingsManager);
        }

        internal static ScriptHostConfiguration ApplyConfiguration(JObject config, ScriptHostEnvironmentSettings environmentSettings,
            ScriptHostConfiguration.Builder configurationBuilder, ScriptSettingsManager settingsManager)
        {
            JArray functions = (JArray)config["functions"];
            if (functions != null && functions.Count > 0)
            {
                foreach (var function in functions)
                {
                    configurationBuilder.AddFunction((string)function);
                }
            }

            // We may already have a host id, but the one from the JSON takes precedence
            string hostId = config["id"]?.ToString();
            if (string.IsNullOrEmpty(hostId))
            {
                hostId = Utility.GetDefaultHostId(settingsManager, environmentSettings.IsSelfHost, environmentSettings.ScriptPath);
            }

            configurationBuilder.WithHostId(hostId);

            JToken fileWatchingEnabled = config["fileWatchingEnabled"];
            if (fileWatchingEnabled != null && fileWatchingEnabled.Type == JTokenType.Boolean)
            {
                configurationBuilder.WithFileWatchingEnabled((bool)fileWatchingEnabled);
            }

            // Configure the set of watched directories, adding the standard built in
            // set to any the user may have specified
            configurationBuilder.AddWatchedDirectory("node_modules");
            JToken watchDirectories = config["watchDirectories"];
            if (watchDirectories != null && watchDirectories.Type == JTokenType.Array)
            {
                foreach (JToken directory in watchDirectories.Where(p => p.Type == JTokenType.String))
                {
                    configurationBuilder.AddWatchedDirectory((string)directory);
                }
            }

            ApplySingletonConfiguration(config, configurationBuilder);

            ApplyTracingConfiguration(config, configurationBuilder);

            // Function timeout
            if (config.TryGetValue("functionTimeout", out JToken value))
            {
                TimeSpan requestedTimeout = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);

                // Only apply limits if this is Dynamic.
                if (settingsManager.IsDynamicSku && (requestedTimeout < MinTimeout || requestedTimeout > MaxTimeout))
                {
                    string message = $"{nameof(ScriptHostConfiguration.FunctionTimeout)} must be between {MinTimeout} and {MaxTimeout}.";
                    throw new ArgumentException(message);
                }

                configurationBuilder.WithFunctionTimeout(requestedTimeout);
            }
            else if (settingsManager.IsDynamicSku)
            {
                // Apply a default if this is running on Dynamic.
                configurationBuilder.WithFunctionTimeout(MaxTimeout);
            }

            ApplySwaggerConfiguration(config, configurationBuilder);

            return configurationBuilder.Build();
        }

        private static void ApplySwaggerConfiguration(JObject config, ScriptHostConfiguration.Builder configurationBuilder)
        {
            var configSection = (JObject)config["swagger"];

            if (configSection != null &&
                configSection.TryGetValue("enabled", out JToken swaggerEnabled) &&
                swaggerEnabled.Type == JTokenType.Boolean &&
                (bool)swaggerEnabled)
            {
                configurationBuilder.EnableSwagger();
            }
        }

        private static JObject ApplyTracingConfiguration(JObject config, ScriptHostConfiguration.Builder configurationBuilder)
        {
            // Apply Tracing/Logging configuration
            var configSection = (JObject)config["tracing"];
            JToken value = null;
            if (configSection != null)
            {
                if (configSection.TryGetValue("consoleLevel", out value))
                {
                    if (Enum.TryParse<TraceLevel>((string)value, true, out TraceLevel consoleLevel))
                    {
                        configurationBuilder.WithConsoleTracingLevel(consoleLevel);
                    }
                }

                if (configSection.TryGetValue("fileLoggingMode", out value))
                {
                    if (Enum.TryParse<FileLoggingMode>((string)value, true, out FileLoggingMode fileLoggingMode))
                    {
                        configurationBuilder.WithFileLoggingMode(fileLoggingMode);
                    }
                }
            }

            return configSection;
        }

        private static void ApplySingletonConfiguration(JObject config, ScriptHostConfiguration.Builder hostConfig)
        {
            // Apply Singleton configuration
            JObject configSection = (JObject)config["singleton"];
            JToken value = null;
            if (configSection != null)
            {
                var singletonConfiguration = new SingletonConfiguration();

                if (configSection.TryGetValue("lockPeriod", out value))
                {
                    singletonConfiguration.LockPeriod = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("listenerLockPeriod", out value))
                {
                    singletonConfiguration.ListenerLockPeriod = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("listenerLockRecoveryPollingInterval", out value))
                {
                    singletonConfiguration.ListenerLockRecoveryPollingInterval = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("lockAcquisitionTimeout", out value))
                {
                    singletonConfiguration.LockAcquisitionTimeout = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }
                if (configSection.TryGetValue("lockAcquisitionPollingInterval", out value))
                {
                    singletonConfiguration.LockAcquisitionPollingInterval = TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
                }

                hostConfig.WithSingletonConfiguration(singletonConfiguration);
            }
        }
    }
}
