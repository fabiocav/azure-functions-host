using DryIoc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceScopingPoc
{
    internal class FunctionsResolver : IDisposable
    {
        public FunctionsResolver(IContainer resolver)
        {
            Container = resolver;
            ChildScopes = new HashSet<FunctionsServiceScope>();
        }

        public IContainer Container { get; }

        public HashSet<FunctionsServiceScope> ChildScopes { get; }

        public void Dispose()
        {
            Task childScopeTasks = Task.WhenAll(ChildScopes.Select(s => s.DisposalTask));
            Task.WhenAny(childScopeTasks, Task.Delay(5000))
                .ContinueWith(t =>
                {
                    Container.Dispose();
                });
        }

        internal FunctionsServiceScope CreateChildScope()
        {
            IResolverContext scopedContext = Container.OpenScope();
            var scope = new FunctionsServiceScope(scopedContext);
            ChildScopes.Add(scope);

            scope.DisposalTask.ContinueWith(t => ChildScopes.Remove(scope));

            return scope;
        }
    }
}
