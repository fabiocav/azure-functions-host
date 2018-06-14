using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ServiceScopingPoc
{
    public class FunctionsServiceScope : IServiceScope
    {
        private readonly TaskCompletionSource<object> _activeTcs;
        private readonly ScopedServiceProvider _serviceProvider;

        public FunctionsServiceScope(IResolverContext serviceProvider)
        {
            _activeTcs = new TaskCompletionSource<object>();
            _serviceProvider = new ScopedServiceProvider(serviceProvider);
        }

        public IServiceProvider ServiceProvider => _serviceProvider;

        public Task DisposalTask => _activeTcs.Task;

        public void Dispose()
        {
            _serviceProvider.Dispose();
            _activeTcs.SetResult(null);
        }
    }
}
