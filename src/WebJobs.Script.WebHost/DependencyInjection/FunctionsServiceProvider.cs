using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace ServiceScopingPoc
{
    public class FunctionsServiceProvider : IServiceProvider, IServiceScopeFactory
    {
        private static readonly Rules _defaultContainerRules;
        private Container _root;
        private FunctionsResolver _currentResolver;

        static FunctionsServiceProvider()
        {
            _defaultContainerRules = Rules.Default
                .With(FactoryMethod.ConstructorWithResolvableArguments)
                .WithFactorySelector(Rules.SelectLastRegisteredFactory())
                .WithTrackingDisposableTransients();
        }

        public FunctionsServiceProvider(IServiceCollection descriptors)
        {
            _root = new Container(rules => _defaultContainerRules);

            _root.Populate(descriptors);
            _root.UseInstance<IServiceProvider>(this);
            _root.UseInstance<FunctionsServiceProvider>(this);

            _currentResolver = new FunctionsResolver(_root);
        }

        public string State { get; set; }

        public IServiceProvider ServiceProvider => throw new NotImplementedException();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider))
            {
                return this;
            }

            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }

            var service = _currentResolver.Container.Resolve(serviceType, IfUnresolved.ReturnDefault);
            string name = serviceType.Name;

            return service;
        }

        public void AddServices(IServiceCollection services)
        {
            _root.Populate(services);
        }

        internal void UpdateChildServices(IServiceCollection serviceDescriptors)
        {
            var rules = _defaultContainerRules
                .WithUnknownServiceResolvers(request => new DelegateFactory(_ => _root.Resolve(request.ServiceType, IfUnresolved.ReturnDefault)));

            var resolver = new Container(rules);
            resolver.Populate(serviceDescriptors);

            var previous = _currentResolver;
            _currentResolver = new FunctionsResolver(resolver);

            if (!ReferenceEquals(previous.Container, _root))
            {
                previous.Dispose();
            }
        }

        public IServiceScope CreateScope()
        {
            return _currentResolver.CreateChildScope();
        }
    }
}
