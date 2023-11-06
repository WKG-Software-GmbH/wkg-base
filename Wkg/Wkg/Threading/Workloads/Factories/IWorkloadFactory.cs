using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public interface IWorkloadFactory<THandle, TFactory>
    where THandle : unmanaged
    where TFactory : AbstractClasslessWorkloadFactory<THandle>, IWorkloadFactory<THandle, TFactory>
{
    internal static abstract TFactory Create(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options);
}
