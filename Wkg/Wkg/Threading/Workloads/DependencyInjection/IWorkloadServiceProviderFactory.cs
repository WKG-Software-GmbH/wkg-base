namespace Wkg.Threading.Workloads.DependencyInjection;

public interface IWorkloadServiceProviderFactory
{
    bool AddService<TService>(Func<TService> factory) where TService : class;

    bool AddService<TInterface, TService>(Func<TService> factory) where TService : class, TInterface;

    IWorkloadServiceProvider GetInstance();
}