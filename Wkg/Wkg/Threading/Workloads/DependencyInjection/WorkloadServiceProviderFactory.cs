namespace Wkg.Threading.Workloads.DependencyInjection;

internal class WorkloadServiceProviderFactory : IWorkloadServiceProviderFactory
{
    private readonly Dictionary<Type, Func<object>> _serviceFactories = new();

    public bool AddService<T>(Func<T> factory) where T : notnull
    {
        if (_serviceFactories.ContainsKey(typeof(T)))
        {
            return false;
        }
        FactoryWrapper<T> wrapper = new(factory);
        _serviceFactories.Add(typeof(T), wrapper.Invoke);
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
