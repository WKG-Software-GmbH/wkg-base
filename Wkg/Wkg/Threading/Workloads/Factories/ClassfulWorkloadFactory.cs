using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public class ClassfulWorkloadFactory<THandle> : AbstractClassfulWorkloadFactory<THandle> where THandle : unmanaged
{
    internal ClassfulWorkloadFactory(IClassfulQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClassfulQdisc<THandle> Root => ClassfulRoot;
}
