using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public class ClasslessWorkloadFactory<THandle> : AbstractClasslessWorkloadFactory<THandle>, IWorkloadFactory<THandle, ClasslessWorkloadFactory<THandle>>, IClasslessWorkloadFactory<THandle>
    where THandle : unmanaged
{
    internal ClasslessWorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClasslessQdisc<THandle> Root => RootRef;

    static ClasslessWorkloadFactory<THandle> IWorkloadFactory<THandle, ClasslessWorkloadFactory<THandle>>
        .Create(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) => 
            new(root, pool, options);
}
