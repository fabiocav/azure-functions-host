using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceScopingPoc.DependencyInjection
{
    public class ScriptHostLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
