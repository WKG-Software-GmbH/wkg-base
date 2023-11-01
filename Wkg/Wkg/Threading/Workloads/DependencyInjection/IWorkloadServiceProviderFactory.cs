namespace Wkg.Threading.Workloads.DependencyInjection;

public interface IWorkloadServiceProviderFactory
{
    bool AddService<T>(Func<T> factory) where T : notnull;
    IWorkloadServiceProvider GetInstance();
}