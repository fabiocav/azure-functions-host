// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Config;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class WebHostEnvironmentSettings : ScriptHostEnvironmentSettings
    {
        public string SecretsPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether authentication/authorization
        /// should be disabled. Useful for local debugging or CLI scenarios.
        /// </summary>
        public bool IsAuthDisabled { get; set; } = false;

        internal static WebHostEnvironmentSettings CreateDefault(ScriptSettingsManager settingsManager)
        {
            WebHostEnvironmentSettings settings = new WebHostEnvironmentSettings
            {
                IsSelfHost = !settingsManager.IsAzureEnvironment
            };

            if (settingsManager.IsAzureEnvironment)
            {
                string home = settingsManager.GetSetting(EnvironmentSettingNames.AzureWebsiteHomePath);
                settings.ScriptPath = Path.Combine(home, @"site\wwwroot");
                settings.LogPath = Path.Combine(home, @"LogFiles\Application\Functions");
                settings.SecretsPath = Path.Combine(home, @"data\Functions\secrets");
            }
            else
            {
                settings.ScriptPath = settingsManager.GetSetting(EnvironmentSettingNames.AzureWebJobsScriptRoot);
                settings.LogPath = Path.Combine(Path.GetTempPath(), @"Functions");
                settings.SecretsPath = System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Secrets");
            }

            if (string.IsNullOrEmpty(settings.ScriptPath))
            {
                throw new InvalidOperationException("Unable to determine function script root directory.");
            }

            return settings;
        }
    }
}