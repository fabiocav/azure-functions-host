// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Azure.WebJobs.Script.Extensibility;

namespace Microsoft.Azure.WebJobs.Script
{
    public class ScriptHostConfiguration
    {
        public ScriptHostConfiguration()
        {
            HostConfig = new JobHostConfiguration();
            FileWatchingEnabled = true;
            FileLoggingMode = FileLoggingMode.Never;
            RootScriptPath = Environment.CurrentDirectory;
            RootLogPath = Path.Combine(Path.GetTempPath(), "Functions");
            LogFilter = new LogCategoryFilter();
            RootExtensionsPath = ConfigurationManager.AppSettings[EnvironmentSettingNames.AzureWebJobsExtensionsPath];
        }

        internal ScriptHostConfiguration(TimeSpan? functionTimeout)
            : this()
        {
            FunctionTimeout = functionTimeout;
        }

        private ScriptHostConfiguration(ScriptHostConfiguration configuration)
        {
            HostConfig = configuration.HostConfig;
            RootScriptPath = configuration.RootScriptPath;
            RootLogPath = configuration.RootLogPath;
            TraceWriter = configuration.TraceWriter;
            FileWatchingEnabled = configuration.FileWatchingEnabled;
            WatchDirectories = configuration.WatchDirectories;
            FileLoggingMode = configuration.FileLoggingMode;
            Functions = configuration.Functions;
            FunctionTimeout = configuration.FunctionTimeout;
            IsSelfHost = configuration.IsSelfHost;
            SwaggerEnabled = configuration.SwaggerEnabled;
        }

        /// <summary>
        /// Gets the <see cref="JobHostConfiguration"/>.
        /// </summary>
        public JobHostConfiguration HostConfig { get; private set; }

        /// <summary>
        /// Gets the path to the script function directory.
        /// </summary>
        public string RootScriptPath { get; private set; }

        /// <summary>
        /// Gets the root path for log files.
        /// </summary>
        public string RootLogPath { get; private set; }

        /// <summary>
        /// Gets or sets the root path to search for binding
        /// extensions.
        /// </summary>
        public string RootExtensionsPath { get; set; }

        /// <summary>
        /// Gets or sets the custom TraceWriter to add to the trace pipeline
        /// </summary>
        public TraceWriter TraceWriter { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="ScriptHost"/> should
        /// monitor file for changes (default is true). When set to true, the host will
        /// automatically react to source/config file changes. When set to false no file
        /// monitoring will be performed.
        /// </summary>
        public bool FileWatchingEnabled { get; private set; }

        /// <summary>
        /// Gets the collection of directories (relative to RootScriptPath) that
        /// should be monitored for changes. If FileWatchingEnabled is true, these directories
        /// will be monitored. When a file is added/modified/deleted in any of these
        /// directories, the host will restart.
        /// </summary>
        public ICollection<string> WatchDirectories { get; private set; }

        /// <summary>
        /// Gets a value governing when logs should be written to disk.
        /// When enabled, logs will be written to the directory specified by
        /// <see cref="RootLogPath"/>.
        /// </summary>
        public FileLoggingMode FileLoggingMode { get; private set; }

        /// <summary>
        /// Gets the list of functions that should be run. This list can be used to filter
        /// the set of functions that will be enabled - it can be a subset of the actual
        /// function directories. When left null (the default) all discovered functions will
        /// be run.
        /// </summary>
        public ICollection<string> Functions { get; private set; }

        /// <summary>
        /// Gets a value indicating the timeout duration for all functions. If null,
        /// there is no timeout duration.
        /// </summary>
        public TimeSpan? FunctionTimeout { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the swagger endpoint is enabled or disabled. If true swagger is enabled, otherwise it is disabled
        /// </summary>
        public bool SwaggerEnabled { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the host is running
        /// outside of the normal Azure hosting environment. E.g. when running
        /// locally or via CLI.
        /// </summary>
        public bool IsSelfHost { get; private set; }

        public Builder ToBuilder() => Builder.FromConfiguration(this);

        public sealed class Builder
        {
            private readonly ScriptHostConfiguration _configuration = new ScriptHostConfiguration();

            public Builder()
            {
            }

            private Builder(ScriptHostConfiguration configuration)
            {
                _configuration = new ScriptHostConfiguration(configuration);
            }

            public static Builder FromConfiguration(ScriptHostConfiguration configuration)
            {
                return new Builder(configuration);
            }

            public Builder WithHostId(string hostId) => UpdateConfiguration(c => c.HostConfig.HostId = hostId);

            public Builder WithRootScriptPath(string path) => UpdateConfiguration(c => c.RootScriptPath = path);

            public Builder WithRootLogPath(string path) => UpdateConfiguration(c => c.RootLogPath = path);

            public Builder WithTraceWriter(TraceWriter traceWriter) => UpdateConfiguration(c => c.TraceWriter = traceWriter);

            public Builder WithFileWatchingEnabled(bool enabled) => UpdateConfiguration(c => c.FileWatchingEnabled = enabled);

            public Builder AddWatchedDirectory(string directory)
            {
                if (_configuration.WatchDirectories == null)
                {
                    _configuration.WatchDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                }

                _configuration.WatchDirectories.Add(directory);

                return this;
            }

            public Builder EnableSwagger() => UpdateConfiguration(c => c.SwaggerEnabled = true);

            public Builder WithFileLoggingMode(FileLoggingMode mode) => UpdateConfiguration(c => c.FileLoggingMode = mode);

            public Builder AddFunction(string functionName)
            {
                return UpdateConfiguration(c =>
                {
                    if (c.Functions == null)
                    {
                        c.Functions = new Collection<string>();
                    }

                    c.Functions.Add(functionName);
                });
            }

            public Builder AddFunctions(IEnumerable<string> functionNames)
            {
                return UpdateConfiguration(c =>
                {
                    if (c.Functions == null)
                    {
                        c.Functions = new Collection<string>();
                    }

                    foreach (var functionName in functionNames)
                    {
                        c.Functions.Add(functionName);
                    }
                });
            }

            public Builder WithFunctionTimeout(TimeSpan timeout) => UpdateConfiguration(c => c.FunctionTimeout = timeout);

            public Builder WithSelfHostValue(bool value) => UpdateConfiguration(c => c.IsSelfHost = value);

            public Builder WithConsoleTracingLevel(TraceLevel level) => UpdateConfiguration(c => c.HostConfig.Tracing.ConsoleLevel = level);

            private Builder UpdateConfiguration(Action<ScriptHostConfiguration> handler)
            {
                handler(_configuration);
                return this;
            }

            public Builder WithSingletonConfiguration(SingletonConfiguration singletonConfiguration)
            {
                _configuration.HostConfig.Singleton.LockPeriod = singletonConfiguration.LockPeriod;
                _configuration.HostConfig.Singleton.ListenerLockPeriod = singletonConfiguration.ListenerLockPeriod;
                _configuration.HostConfig.Singleton.ListenerLockRecoveryPollingInterval = singletonConfiguration.ListenerLockRecoveryPollingInterval;
                _configuration.HostConfig.Singleton.LockAcquisitionTimeout = singletonConfiguration.LockAcquisitionTimeout;
                _configuration.HostConfig.Singleton.LockAcquisitionPollingInterval = singletonConfiguration.LockAcquisitionPollingInterval;

                return this;
            }

            public ScriptHostConfiguration Build()
            {
                return new ScriptHostConfiguration(_configuration);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="LogCategoryFilter"/> to use when constructing providers for the
        /// registered <see cref="ILoggerFactory"/>.
        /// </summary>
        public LogCategoryFilter LogFilter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SamplingPercentageEstimatorSettings"/> to be used for Application
        /// Insights client-side sampling. If null, client-side sampling is disabled.
        /// </summary>
        public SamplingPercentageEstimatorSettings ApplicationInsightsSamplingSettings { get; set; }
    }
}
