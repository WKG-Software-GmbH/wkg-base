namespace Wkg.Threading.Workloads.DependencyInjection.Configuration;

public class WorkloadServiceProviderBuilder(IWorkloadServiceProviderFactory factory)
{
    public WorkloadServiceProviderBuilder AddService<T>(Func<T> serviceFactory) where T : class =>
        AddService<T, T>(serviceFactory);

    public WorkloadServiceProviderBuilder AddService<TInterface, TImplementation>(Func<TImplementation> serviceFactory) where TImplementation : class, TInterface
    {
        factory.AddService<TInterface, TImplementation>(serviceFactory);
        return this;
    }

    public WorkloadServiceProviderBuilder AddSingleton<T>(T singleton) where T : class =>
        AddSingleton<T, T>(singleton);

    public WorkloadServiceProviderBuilder AddSingleton<TInterface, TImplementation>(TImplementation singleton) where TImplementation : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(singleton, nameof(singleton));

        factory.AddService<TInterface, TImplementation>(() => singleton);
        return this;
    }

    internal IWorkloadServiceProviderFactory Build() => factory;
}
