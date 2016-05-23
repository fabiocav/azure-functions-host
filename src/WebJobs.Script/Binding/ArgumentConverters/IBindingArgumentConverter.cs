// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Description;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    public interface IBindingArgumentConverter
    {
        Task<object> ConvertToValueAsync(DataType valueType, object argument, FunctionBinding binding, InvocationContext context);

        Task<T> ConvertFromValueAsync<T>(object value, DataType valueType, FunctionBinding binding, InvocationContext context);

        Task<object> ConvertFromValueAsync(Type argumentType, object value, DataType valueType, FunctionBinding binding, InvocationContext context);

        bool CanConvert(Type argumentType, DataType functionDataType);
    }
}
