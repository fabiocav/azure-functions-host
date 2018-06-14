using Microsoft.Extensions.DependencyInjection;
using System;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        private FunctionsServiceProvider _provider;

        public FunctionsServiceProviderFactory()
        {

        }
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            if (_provider == null)
            {
                _provider = new FunctionsServiceProvider(containerBuilder);
            }
            else
            {
                _provider.AddServices(containerBuilder);
            }

            return _provider;
        }
    }
}
