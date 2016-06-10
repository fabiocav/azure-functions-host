// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public sealed class TimerInfoBindingArgumentConverter : IBindingArgumentConverter
    {
        public bool CanConvert(Type argumentType, DataType functionDataType)
        {
            return argumentType == typeof(TimerInfo);
        }

        public Task<object> ConvertFromValueAsync(Type argumentType, object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            throw new NotSupportedException("This converter only supports input bindings and triggers.");
        }

        public Task<T> ConvertFromValueAsync<T>(object value, DataType valueType, FunctionBinding binding, InvocationContext context)
        {
            throw new NotSupportedException("This converter only supports input bindings and triggers.");
        }

        public Task<object> ConvertToValueAsync(DataType valueType, object argument, FunctionBinding binding, InvocationContext context)
        {
            TimerInfo timerInfo = (TimerInfo)argument;
            var result = new Dictionary<string, object>()
                {
                    { "isPastDue", timerInfo.IsPastDue }
                };

            if (timerInfo.ScheduleStatus != null)
            {
                result["last"] = timerInfo.ScheduleStatus.Last.ToString("s", CultureInfo.InvariantCulture);
                result["next"] = timerInfo.ScheduleStatus.Next.ToString("s", CultureInfo.InvariantCulture);
            }

            return Task.FromResult<object>(result);
        }
    }
}
