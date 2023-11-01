using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public class ClassifyingWorkloadFactory<THandle> : AbstractClassifyingWorkloadFactory<THandle> where THandle : unmanaged
{
    internal ClassifyingWorkloadFactory(IClassifyingQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    // we know that the root is classful, so we can safely do this
    public IClassifyingQdisc<THandle> Root => ClassifyingRoot;
}
