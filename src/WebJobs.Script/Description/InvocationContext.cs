// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.Azure.WebJobs.Script.Description
{
    public class InvocationContext
    {
        public InvocationContext()
            : this(new Collection<BindingArgument>(), new Dictionary<string, object>())
        {
        }

        public InvocationContext(ICollection<BindingArgument> bindingArguments, IDictionary<string, object> executionContext)
        {
            BindingArguments = bindingArguments;
            ExecutionContext = executionContext;
        }

        public ICollection<BindingArgument> BindingArguments { get; }

        public IDictionary<string, object> ExecutionContext { get; }
    }
}
