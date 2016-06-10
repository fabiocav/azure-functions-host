// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;
using NCrontab;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public class TimerBinding : FunctionBinding
    {
        private readonly bool _runOnStartup;
        private readonly string _schedule;

        public TimerBinding(ScriptHostConfiguration config, TimerBindingMetadata metadata, FileAccess access) : base(config, metadata, access)
        {
            _schedule = metadata.Schedule;
            _runOnStartup = metadata.RunOnStartup;
        }

        public override Type GetArgumentType()
        {
            return typeof(TimerInfo);
        }

        public override Collection<CustomAttributeBuilder> GetCustomAttributes(Type parameterType)
        {
            CrontabSchedule.ParseOptions options = new CrontabSchedule.ParseOptions()
            {
                IncludingSeconds = true
            };

            if (CrontabSchedule.TryParse(_schedule, options) == null)
            {
                throw new ArgumentException(string.Format("'{0}' is not a valid CRON expression.", _schedule));
            }

            ConstructorInfo ctorInfo = typeof(TimerTriggerAttribute).GetConstructor(new Type[] { typeof(string) });

            PropertyInfo runOnStartupProperty = typeof(TimerTriggerAttribute).GetProperty("RunOnStartup");
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(ctorInfo,
                new object[] { _schedule },
                new PropertyInfo[] { runOnStartupProperty },
                new object[] { _runOnStartup });

            return new Collection<CustomAttributeBuilder>
            {
                attributeBuilder
            };
        }
    }
}
