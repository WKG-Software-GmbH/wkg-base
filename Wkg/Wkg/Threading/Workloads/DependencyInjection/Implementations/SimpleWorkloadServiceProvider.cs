using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

internal class SimpleWorkloadServiceProvider : WorkloadServiceProviderBase
{
    private protected readonly Dictionary<Type, Func<object>> _servicesFactories = new();
    private protected readonly Dictionary<Type, object> _services = new();
    private bool disposedValue;

    public SimpleWorkloadServiceProvider(IEnumerable<KeyValuePair<Type, Func<object>>> serviceFactories)
    {
        foreach ((Type type, Func<object> factory) in serviceFactories)
        {
            _servicesFactories.Add(type, factory);
        }
    }

    public override T GetRequiredService<T>()
    {
        if (TryGetService(out T? service))
        {
            return service;
        }

        throw new InvalidOperationException($"Service of type {typeof(T)} is not registered.");
    }

    public override object GetRequiredService(Type serviceType)
    {
        if (TryGetService(serviceType, out object? service))
        {
            return service;
        }

        throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
    }

    public override object? GetService(Type serviceType) =>
        TryGetService(serviceType, out object? service) ? service : default;

    public override bool TryGetService<T>([NotNullWhen(true)] out T? service) where T : default
    {
        bool result = TryGetService(typeof(T), out object? serviceObject);
        service = (T?)serviceObject;
        return result;
    }

    public override bool TryGetService(Type serviceType, [NotNullWhen(true)] out object? service)
    {
        if (_services.TryGetValue(serviceType, out object? value))
        {
            service = value;
            return true;
        }
        if (_servicesFactories.TryGetValue(serviceType, out Func<object>? factory))
        {
            service = factory.Invoke();
            _services.Add(serviceType, service);
            return true;
        }
        service = default;
        return false;
    }

    protected override void Initialize()
    {
        foreach ((Type type, Func<object> factory) in _servicesFactories)
        {
            _services.Add(type, factory.Invoke());
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (object service in _services.Values)
                {
                    if (service is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            _services.Clear();
            _servicesFactories.Clear();
            disposedValue = true;
        }
    }

    public override void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
