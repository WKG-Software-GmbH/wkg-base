namespace Wkg.Threading.Workloads.DependencyInjection;

internal class WorkloadServiceProviderFactory : IWorkloadServiceProviderFactory
{
    private readonly Dictionary<Type, Func<object>> _serviceFactories = new();

    public bool AddService<TService>(Func<TService> factory) where TService : class
    {
        if (_serviceFactories.ContainsKey(typeof(TService)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TService), wrapper.Invoke);
        return true;
    }

    public bool AddService<TInterface, TService>(Func<TService> factory) where TService : class, TInterface
    {
        if (_serviceFactories.ContainsKey(typeof(TInterface)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TInterface), wrapper.Invoke);
        return true;
    }

    public IWorkloadServiceProvider GetInstance() => new WorkloadServiceProvider(_serviceFactories);

    private class FactoryWrapper<T> where T : notnull
    {
        private readonly Func<T> _factory;

        public FactoryWrapper(Func<T> factory)
        {
            _factory = factory;
        }

        public object Invoke() => _factory.Invoke();
    }
}
