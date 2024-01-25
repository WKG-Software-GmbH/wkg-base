namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

internal class PooledWorkloadServiceProvider(PooledWorkloadServiceProviderFactory _pool, IEnumerable<KeyValuePair<Type, Func<object>>> serviceFactories) 
    : SimpleWorkloadServiceProvider(serviceFactories)
{
    public override void Dispose() => _pool.ReturnInstance(this);
}
