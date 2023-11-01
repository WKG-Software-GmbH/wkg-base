using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.DependencyInjection;

public abstract class WorkloadServiceProviderBase : IWorkloadServiceProvider
{
    public abstract T GetRequiredService<T>();

    public abstract object GetRequiredService(Type serviceType);

    public abstract object? GetService(Type serviceType);

    public abstract bool TryGetService<T>([NotNullWhen(true)] out T? service);

    public abstract bool TryGetService(Type serviceType, [NotNullWhen(true)] out object? service);

    protected abstract void Initialize();

    void IWorkloadServiceProvider.Initialize() => Initialize();

    public abstract void Dispose();
}
