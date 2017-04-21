﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs.Script.Config
{
    public class ScriptHostEnvironmentSettings
    {
        public string ScriptPath { get; set; }

        public string LogPath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the host is running
        /// outside of the normal Azure hosting environment. E.g. when running
        /// locally or via CLI.
        /// </summary>
        public bool IsSelfHost { get; set; }

        public TraceWriter TraceWriter { get; set; }

        public virtual ScriptHostConfiguration.Builder Build(ScriptHostConfiguration.Builder configurationBuilder)
        {
            return configurationBuilder.WithRootScriptPath(ScriptPath)
                .WithRootLogPath(LogPath)
                .WithFileLoggingMode(FileLoggingMode.DebugOnly)
                .WithTraceWriter(TraceWriter)
                .WithSelfHostValue(IsSelfHost);
        }
    }
}