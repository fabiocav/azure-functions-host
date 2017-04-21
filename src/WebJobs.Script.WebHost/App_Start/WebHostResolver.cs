﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Config;
using Microsoft.Azure.WebJobs.Script.Diagnostics;
using Microsoft.Azure.WebJobs.Script.WebHost.WebHooks;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public sealed class WebHostResolver : IDisposable
    {
        private static object _syncLock = new object();

        private readonly ISecretManagerFactory _secretManagerFactory;
        private WebScriptHostManager _standbyHostManager;
        private WebHookReceiverManager _standbyReceiverManager;

        private WebScriptHostManager _activeHostManager;
        private WebHookReceiverManager _activeReceiverManager;

        private static ScriptSettingsManager _settingsManager;

        public WebHostResolver(ScriptSettingsManager settingsManager, ISecretManagerFactory secretManagerFactory)
        {
            _settingsManager = settingsManager;
            _secretManagerFactory = secretManagerFactory;
        }

        public ISwaggerDocumentManager GetSwaggerDocumentManager(WebHostEnvironmentSettings settings)
        {
            return GetWebScriptHostManager(settings).SwaggerDocumentManager;
        }

        public ISecretManager GetSecretManager(WebHostEnvironmentSettings settings)
        {
            return GetWebScriptHostManager(settings).SecretManager;
        }

        public HostPerformanceManager GetPerformanceManager(WebHostEnvironmentSettings settings)
        {
            return GetWebScriptHostManager(settings).PerformanceManager;
        }

        public WebScriptHostManager GetWebScriptHostManager(WebHostEnvironmentSettings settings)
        {
            if (_activeHostManager != null)
            {
                return _activeHostManager;
            }

            lock (_syncLock)
            {
                EnsureInitialized(settings);

                return _activeHostManager ?? _standbyHostManager;
            }
        }

        public WebHookReceiverManager GetWebHookReceiverManager(WebHostEnvironmentSettings settings)
        {
            if (_activeReceiverManager != null)
            {
                return _activeReceiverManager;
            }

            lock (_syncLock)
            {
                EnsureInitialized(settings);

                return _activeReceiverManager ?? _standbyReceiverManager;
            }
        }

        private void EnsureInitialized(WebHostEnvironmentSettings settings)
        {
            // standby mode can only change from true to false
            // When standby mode changes, we reset all instances
            var standbyMode = WebScriptHostManager.InStandbyMode;
            if (!standbyMode)
            {
                if (_activeHostManager == null)
                {
                    if (_standbyHostManager != null)
                    {
                        // reintialize app settings if we were in standby
                        ReinitializeAppSettings();
                    }

                    InitializeFileSystem(settings.ScriptPath);

                    _activeHostManager = new WebScriptHostManager(_secretManagerFactory, _settingsManager, settings);
                    _activeReceiverManager = new WebHookReceiverManager(_activeHostManager.SecretManager);

                    _standbyHostManager?.Dispose();
                    _standbyReceiverManager?.Dispose();

                    _standbyHostManager = null;
                    _standbyReceiverManager = null;
                    _settingsManager.Reset();
                }
            }
            else
            {
                if (_standbyHostManager == null)
                {
                    InitializeFileSystem(settings.ScriptPath);

                    _standbyHostManager = new WebScriptHostManager(_secretManagerFactory, _settingsManager, settings);
                    _standbyReceiverManager = new WebHookReceiverManager(_standbyHostManager.SecretManager);
                }
            }
        }

        private static void ReinitializeAppSettings()
        {
            if (_settingsManager.IsAzureEnvironment)
            {
                // the nature of this is only add or update (not remove).
                // so there may be settings from standby site leak over.
                var assembly = AppDomain.CurrentDomain.GetAssemblies().First(a => a.FullName.StartsWith("EnvSettings, "));
                var envSettingType = assembly.GetType("EnvSettings.SettingsProcessor", throwOnError: true);
                var startMethod = envSettingType.GetMethod("Start", BindingFlags.Public | BindingFlags.Static);
                startMethod.Invoke(null, new object[0]);
            }
        }

        private static void InitializeFileSystem(string scriptPath)
        {
            if (ScriptSettingsManager.Instance.IsAzureEnvironment)
            {
                // When running on Azure, we kick this off on the background
                Task.Run(() =>
                {
                    string home = ScriptSettingsManager.Instance.GetSetting(EnvironmentSettingNames.AzureWebsiteHomePath);
                    if (!string.IsNullOrEmpty(home))
                    {
                        // Delete hostingstart.html if any. Azure creates that in all sites by default
                        string hostingStart = Path.Combine(scriptPath, "hostingstart.html");
                        if (File.Exists(hostingStart))
                        {
                            File.Delete(hostingStart);
                        }

                        // Create the tools folder if it doesn't exist
                        string toolsPath = Path.Combine(home, @"site\tools");
                        Directory.CreateDirectory(toolsPath);

                        var folders = new List<string>();
                        folders.Add(Path.Combine(home, @"site\tools"));

                        string path = Environment.GetEnvironmentVariable("PATH");
                        string additionalPaths = string.Join(";", folders);

                        // Make sure we haven't already added them. This can happen if the appdomain restart (since it's still same process)
                        if (!path.Contains(additionalPaths))
                        {
                            path = additionalPaths + ";" + path;

                            Environment.SetEnvironmentVariable("PATH", path);
                        }
                    }
                });
            }
            else
            {
                // Ensure we have our scripts directory in non-Azure scenarios
                Directory.CreateDirectory(scriptPath);
            }
        }

        public void Dispose()
        {
            _standbyHostManager?.Dispose();
            _standbyReceiverManager?.Dispose();

            _activeHostManager?.Dispose();
            _activeReceiverManager?.Dispose();
        }
    }
}