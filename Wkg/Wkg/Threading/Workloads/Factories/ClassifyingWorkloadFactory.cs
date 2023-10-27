using Wkg.Threading.Workloads.Queuing.Classifiers;

namespace Wkg.Threading.Workloads.Factories;

public class ClassifyingWorkloadFactory<THandle> : AbstractClassifyingWorkloadFactory<THandle> where THandle : unmanaged
{
    internal protected ClassifyingWorkloadFactory(IClassifyingQdisc<THandle> root) : base(root)
    {
    }

    // we know that the root is classful, so we can safely do this
    public IClassifyingQdisc<THandle> Root => ClassifyingRoot;
}
