// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Script.Binding;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    public sealed class BindingArgument
    {
        public BindingArgument(FunctionBinding binding, object value)
        {
            Binding = binding;
            Input = value;
        }

        public object Input { get; private set; }

        public FunctionBinding Binding { get; private set; }
    }
}
