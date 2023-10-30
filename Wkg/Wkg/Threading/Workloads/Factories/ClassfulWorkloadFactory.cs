using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classful;

namespace Wkg.Threading.Workloads.Factories;

public class ClassfulWorkloadFactory<THandle> : AbstractClassfulWorkloadFactory<THandle> where THandle : unmanaged
{
    internal ClassfulWorkloadFactory(IClassfulQdisc<THandle> root, AnonymousWorkloadPool? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClassfulQdisc<THandle> Root => ClassfulRoot;
}
