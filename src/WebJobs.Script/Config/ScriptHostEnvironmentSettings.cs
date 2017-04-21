// Copyright (c) .NET Foundation. All rights reserved.
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

        public TraceWriter TraceWriter { get; set; }
    }
}
