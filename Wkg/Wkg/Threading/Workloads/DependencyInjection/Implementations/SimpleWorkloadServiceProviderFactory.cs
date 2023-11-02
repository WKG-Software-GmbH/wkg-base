namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

/// <summary>
/// A simple implementation of <see cref="IWorkloadServiceProviderFactory"/> that allows services to be persisted and reused across workloads for the lifetime of a worker thread.
/// Once a worker thread is terminated, all services are disposed.
/// </summary>
public class SimpleWorkloadServiceProviderFactory : WorkloadServiceProviderFactory, IWorkloadServiceProviderFactory
{
    private readonly Dictionary<Type, Func<object>> _serviceFactories = new();

    bool IWorkloadServiceProviderFactory.AddService<TService>(Func<TService> factory)
    {
        if (_serviceFactories.ContainsKey(typeof(TService)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TService), wrapper.Invoke);
        return true;
    }

    bool IWorkloadServiceProviderFactory.AddService<TInterface, TService>(Func<TService> factory)
    {
        if (_serviceFactories.ContainsKey(typeof(TInterface)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TInterface), wrapper.Invoke);
        return true;
    }

    IWorkloadServiceProvider IWorkloadServiceProviderFactory.GetInstance() => new SimpleWorkloadServiceProvider(_serviceFactories);
}
