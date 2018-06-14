using DryIoc;
using System;

namespace ServiceScopingPoc
{
    public class ScopedServiceProvider : IServiceProvider, IDisposable
    {
        private readonly IResolverContext _resolver;

        public ScopedServiceProvider(IResolverContext container)
        {
            _resolver = container;
        }

        public void Dispose()
        {
            _resolver.Dispose();
        }

        public object GetService(Type serviceType)
        {
            return _resolver.Resolve(serviceType, IfUnresolved.ReturnDefault);
        }
    }
}
