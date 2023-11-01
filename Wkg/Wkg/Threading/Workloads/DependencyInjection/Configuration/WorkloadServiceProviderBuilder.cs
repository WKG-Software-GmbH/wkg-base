using Wkg.Common.ThrowHelpers;

namespace Wkg.Threading.Workloads.DependencyInjection.Configuration;

public class WorkloadServiceProviderBuilder
{
    private readonly IWorkloadServiceProviderFactory _factory;

    public WorkloadServiceProviderBuilder(IWorkloadServiceProviderFactory factory) => _factory = factory;

    public WorkloadServiceProviderBuilder AddService<T>(Func<T> serviceFactory) where T : class =>
        AddService<T, T>(serviceFactory);

    public WorkloadServiceProviderBuilder AddService<TInterface, TImplementation>(Func<TImplementation> serviceFactory) where TImplementation : class, TInterface
    {
        _factory.AddService<TInterface, TImplementation>(serviceFactory);
        return this;
    }

    public WorkloadServiceProviderBuilder AddSingleton<T>(T singleton) where T : class =>
        AddSingleton<T, T>(singleton);

    public WorkloadServiceProviderBuilder AddSingleton<TInterface, TImplementation>(TImplementation singleton) where TImplementation : class, TInterface
    {
        Throw.ArgumentNullException.IfNull(singleton, nameof(singleton));

        _factory.AddService<TInterface, TImplementation>(() => singleton);
        return this;
    }

    internal IWorkloadServiceProviderFactory Build() => _factory;
}
