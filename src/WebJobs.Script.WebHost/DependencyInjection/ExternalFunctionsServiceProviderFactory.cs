using Microsoft.Extensions.DependencyInjection;
using System;

namespace ServiceScopingPoc
{
    internal class ExternalFunctionsServiceProviderFactory : IServiceProviderFactory<FunctionsServiceProvider>
    {
        private FunctionsServiceProvider _provider;

        public ExternalFunctionsServiceProviderFactory(FunctionsServiceProvider provider)
        {
            _provider = provider;
        }

        public FunctionsServiceProvider CreateBuilder(IServiceCollection services)
        {
            _provider.UpdateChildServices(services);
            return _provider;
        }

        public IServiceProvider CreateServiceProvider(FunctionsServiceProvider containerBuilder)
        {
            return containerBuilder;
        }
    }
}
