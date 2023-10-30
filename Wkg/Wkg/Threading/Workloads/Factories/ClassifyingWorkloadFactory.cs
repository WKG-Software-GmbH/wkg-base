using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classifiers;

namespace Wkg.Threading.Workloads.Factories;

public class ClassifyingWorkloadFactory<THandle> : AbstractClassifyingWorkloadFactory<THandle> where THandle : unmanaged
{
    internal ClassifyingWorkloadFactory(IClassifyingQdisc<THandle> root, AnonymousWorkloadPool? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    // we know that the root is classful, so we can safely do this
    public IClassifyingQdisc<THandle> Root => ClassifyingRoot;
}
