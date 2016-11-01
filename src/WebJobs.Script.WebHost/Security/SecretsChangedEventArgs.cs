using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class SecretsChangedEventArgs : EventArgs
    {
        public ScriptSecretsType Type { get; set; }

        public string Name { get; set; }
    }
}