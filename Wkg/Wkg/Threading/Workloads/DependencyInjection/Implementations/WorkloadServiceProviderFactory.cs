namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

public abstract class WorkloadServiceProviderFactory
{
    protected class FactoryWrapper<T> where T : notnull
    {
        private readonly Func<T> _factory;

        public FactoryWrapper(Func<T> factory)
        {
            _factory = factory;
        }

        public object Invoke() => _factory.Invoke();
    }
}
