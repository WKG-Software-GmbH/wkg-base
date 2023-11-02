using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

internal class PooledWorkloadServiceProvider : SimpleWorkloadServiceProvider
{
    private readonly PooledWorkloadServiceProviderFactory _pool;

    public PooledWorkloadServiceProvider(PooledWorkloadServiceProviderFactory pool, IEnumerable<KeyValuePair<Type, Func<object>>> serviceFactories) : base(serviceFactories)
    {
        _pool = pool;
    }

    public override void Dispose() => _pool.ReturnInstance(this);
}
