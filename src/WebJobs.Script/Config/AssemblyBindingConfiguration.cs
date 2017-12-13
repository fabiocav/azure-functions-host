// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.Azure.WebJobs.Script
{
    public class AssemblyBindingConfiguration
    {
        public AssemblyBindingConfiguration()
        {
            Redirects = ImmutableArray<AssemblyBindingRedirect>.Empty;
            MatchDeployedAssembliesByName = false;
        }

        /// <summary>
        /// Gets or sets the collection of binding redirects configured.
        /// </summary>
        public ImmutableArray<AssemblyBindingRedirect> Redirects { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the runtime should match deployed
        /// assemblies by simple name, regardless of version.
        /// </summary>
        public bool MatchDeployedAssembliesByName { get; set; }
    }
}