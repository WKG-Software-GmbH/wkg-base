namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

public abstract class WorkloadServiceProviderFactory
{
    protected class FactoryWrapper<T>(Func<T> factory) where T : notnull
    {
        public object Invoke() => factory.Invoke();
    }
}
