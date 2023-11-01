using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.DependencyInjection;

public interface IWorkloadServiceProvider : IServiceProvider, IDisposable
{
    public T GetRequiredService<T>();

    public object GetRequiredService(Type serviceType);

    public bool TryGetService<T>([NotNullWhen(true)] out T? service);

    public bool TryGetService(Type serviceType, [NotNullWhen(true)] out object? service);

    internal void Initialize();
}
