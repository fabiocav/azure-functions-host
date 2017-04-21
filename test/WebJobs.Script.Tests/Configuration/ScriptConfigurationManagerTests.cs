// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs.Script.Config;
using Newtonsoft.Json.Linq;
using Xunit;
using ScriptConfigurationBuilder = Microsoft.Azure.WebJobs.Script.ScriptHostConfiguration.Builder;

namespace Microsoft.Azure.WebJobs.Script.Tests.Configuration
{
    public class ScriptConfigurationManagerTests
    {
        private const string ID = "5a709861cab44e68bfed5d2c2fe7fc0c";
        private ScriptSettingsManager _settingsManager = ScriptSettingsManager.Instance;

        [Fact]
        public void ApplyConfiguration_NoTimeoutLimits_IfNotDynamic()
        {
            var config = new JObject
            {
                ["id"] = ID
            };
            var scriptConfig = new ScriptConfigurationBuilder();

            config["functionTimeout"] = "00:05:01";
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), scriptConfig, new ScriptSettingsManager());
            Assert.Equal(TimeSpan.FromSeconds(301), scriptConfig.Build().FunctionTimeout);

            config["functionTimeout"] = "00:00:00.9";
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), scriptConfig, new ScriptSettingsManager());
            Assert.Equal(TimeSpan.FromMilliseconds(900), scriptConfig.Build().FunctionTimeout);
        }

        [Fact]
        public void ApplyConfiguration_TopLevel()
        {
            JObject config = new JObject();
            config["id"] = ID;
            var builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());

            Assert.Equal(ID, builder.Build().HostConfig.HostId);
        }

        [Fact]
        public void BuildHostConfiguration_InvalidHostJson_ThrowsInformativeException()
        {
            string rootPath = Path.Combine(Environment.CurrentDirectory, @"TestScripts\Invalid");

            var environmentSettings = new ScriptHostEnvironmentSettings
            {
                ScriptPath = rootPath,
                IsSelfHost = true
            };

            var ex = Assert.Throws<FormatException>(() =>
            {
                ScriptConfigurationManager.BuildHostConfiguration(_settingsManager, environmentSettings);
            });

            Assert.Equal(string.Format("Unable to parse {0} file.", ScriptConstants.HostMetadataFileName), ex.Message);
            Assert.Equal("Invalid property identifier character: ~. Path '', line 2, position 4.", ex.InnerException.Message);
        }

        [Fact]
        public void ApplyConfiguration_Singleton()
        {
            JObject config = new JObject();
            config["id"] = ID;
            JObject singleton = new JObject();
            config["singleton"] = singleton;

            var builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());

            ScriptHostConfiguration scriptConfig = builder.Build();

            Assert.Equal(ID, scriptConfig.HostConfig.HostId);
            Assert.Equal(15, scriptConfig.HostConfig.Singleton.LockPeriod.TotalSeconds);
            Assert.Equal(1, scriptConfig.HostConfig.Singleton.ListenerLockPeriod.TotalMinutes);
            Assert.Equal(1, scriptConfig.HostConfig.Singleton.ListenerLockRecoveryPollingInterval.TotalMinutes);
            Assert.Equal(TimeSpan.MaxValue, scriptConfig.HostConfig.Singleton.LockAcquisitionTimeout);
            Assert.Equal(5, scriptConfig.HostConfig.Singleton.LockAcquisitionPollingInterval.TotalSeconds);

            singleton["lockPeriod"] = "00:00:17";
            singleton["listenerLockPeriod"] = "00:00:22";
            singleton["listenerLockRecoveryPollingInterval"] = "00:00:33";
            singleton["lockAcquisitionTimeout"] = "00:05:00";
            singleton["lockAcquisitionPollingInterval"] = "00:00:08";

            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());

            scriptConfig = builder.Build();
            Assert.Equal(17, scriptConfig.HostConfig.Singleton.LockPeriod.TotalSeconds);
            Assert.Equal(22, scriptConfig.HostConfig.Singleton.ListenerLockPeriod.TotalSeconds);
            Assert.Equal(33, scriptConfig.HostConfig.Singleton.ListenerLockRecoveryPollingInterval.TotalSeconds);
            Assert.Equal(5, scriptConfig.HostConfig.Singleton.LockAcquisitionTimeout.TotalMinutes);
            Assert.Equal(8, scriptConfig.HostConfig.Singleton.LockAcquisitionPollingInterval.TotalSeconds);
        }

        // with swagger with setting name with value
        // with swagger with setting name with wrong value set
        [Fact]
        public void ApplyConfiguration_Swagger()
        {
            JObject config = new JObject();
            config["id"] = ID;

            // no swagger section
            var builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            ScriptHostConfiguration scriptConfig = builder.Build();
            Assert.Equal(false, scriptConfig.SwaggerEnabled);

            // empty swagger section
            JObject swagger = new JObject();
            config["swagger"] = swagger;
            builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(false, scriptConfig.SwaggerEnabled);

            // swagger section present, with swagger mode set to null
            swagger["enabled"] = string.Empty;
            builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(false, scriptConfig.SwaggerEnabled);

            // swagger section present, with swagger mode set to true
            swagger["enabled"] = true;
            builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(true, scriptConfig.SwaggerEnabled);

            // swagger section present, with swagger mode set to invalid
            swagger["enabled"] = "invalid";
            builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(false, scriptConfig.SwaggerEnabled);
        }

        [Fact]
        public void ApplyConfiguration_Tracing()
        {
            JObject config = new JObject();
            config["id"] = ID;
            JObject tracing = new JObject();
            config["tracing"] = tracing;
            var builder = new ScriptConfigurationBuilder();
            ScriptHostConfiguration scriptConfig = builder.Build();

            Assert.Equal(TraceLevel.Info, scriptConfig.HostConfig.Tracing.ConsoleLevel);
            Assert.Equal(FileLoggingMode.Never, scriptConfig.FileLoggingMode);

            tracing["consoleLevel"] = "Verbose";
            tracing["fileLoggingMode"] = "Always";

            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(TraceLevel.Verbose, scriptConfig.HostConfig.Tracing.ConsoleLevel);
            Assert.Equal(FileLoggingMode.Always, scriptConfig.FileLoggingMode);

            tracing["fileLoggingMode"] = "DebugOnly";
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(FileLoggingMode.DebugOnly, scriptConfig.FileLoggingMode);
        }

        [Fact]
        public void ApplyConfiguration_FileWatching()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();
            ScriptHostConfiguration scriptConfig = builder.Build();
            Assert.True(scriptConfig.FileWatchingEnabled);

            scriptConfig = new ScriptHostConfiguration();
            config["fileWatchingEnabled"] = new JValue(true);
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.True(scriptConfig.FileWatchingEnabled);
            Assert.Equal(1, scriptConfig.WatchDirectories.Count);
            Assert.Equal("node_modules", scriptConfig.WatchDirectories.ElementAt(0));

            scriptConfig = new ScriptHostConfiguration();
            config["fileWatchingEnabled"] = new JValue(false);
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.False(scriptConfig.FileWatchingEnabled);

            scriptConfig = new ScriptHostConfiguration();
            config["fileWatchingEnabled"] = new JValue(true);
            config["watchDirectories"] = new JArray("Shared", "Tools");
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.True(scriptConfig.FileWatchingEnabled);
            Assert.Equal(3, scriptConfig.WatchDirectories.Count);
            Assert.Equal("node_modules", scriptConfig.WatchDirectories.ElementAt(0));
            Assert.Equal("Shared", scriptConfig.WatchDirectories.ElementAt(1));
            Assert.Equal("Tools", scriptConfig.WatchDirectories.ElementAt(2));
        }

        [Fact]
        public void ApplyConfiguration_AppliesFunctionsFilter()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();
            ScriptHostConfiguration scriptConfig = builder.Build();
            Assert.Null(scriptConfig.Functions);

            config["functions"] = new JArray("Function1", "Function2");

            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(2, scriptConfig.Functions.Count);
            Assert.Equal("Function1", scriptConfig.Functions.ElementAt(0));
            Assert.Equal("Function2", scriptConfig.Functions.ElementAt(1));
        }

        [Fact]
        public void ApplyConfiguration_AppliesTimeout()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();
            ScriptHostConfiguration scriptConfig = builder.Build();
            Assert.Null(scriptConfig.FunctionTimeout);

            config["functionTimeout"] = "00:00:30";

            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            scriptConfig = builder.Build();
            Assert.Equal(TimeSpan.FromSeconds(30), scriptConfig.FunctionTimeout);
        }

        [Fact]
        public void ApplyConfiguration_TimeoutDefaultsNull_IfNotDynamic()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();
            ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
            ScriptHostConfiguration scriptConfig = builder.Build();

            Assert.Null(scriptConfig.FunctionTimeout);
        }

        [Fact]
        public void ApplyConfiguration_TimeoutDefaults5Minutes_IfDynamic()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();

            try
            {
                _settingsManager.SetSetting(EnvironmentSettingNames.AzureWebsiteSku, "Dynamic");
                ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager());
                ScriptHostConfiguration scriptConfig = builder.Build();
                Assert.Equal(TimeSpan.FromMinutes(5), scriptConfig.FunctionTimeout);
            }
            finally
            {
                _settingsManager.SetSetting(EnvironmentSettingNames.AzureWebsiteSku, null);
            }
        }

        [Fact]
        public void ApplyConfiguration_AppliesTimeoutLimits_IfDynamic()
        {
            JObject config = new JObject();
            config["id"] = ID;

            var builder = new ScriptConfigurationBuilder();

            try
            {
                _settingsManager.SetSetting(EnvironmentSettingNames.AzureWebsiteSku, "Dynamic");

                config["functionTimeout"] = "00:05:01";
                Assert.Throws<ArgumentException>(() => ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager()));

                config["functionTimeout"] = "00:00:00.9";
                Assert.Throws<ArgumentException>(() => ScriptConfigurationManager.ApplyConfiguration(config, new ScriptHostEnvironmentSettings(), builder, new ScriptSettingsManager()));
            }
            finally
            {
                _settingsManager.SetSetting(EnvironmentSettingNames.AzureWebsiteSku, null);
            }
        }
    }
}
