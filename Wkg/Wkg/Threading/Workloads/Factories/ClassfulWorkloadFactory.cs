using Wkg.Threading.Workloads.Queuing.Classful;

namespace Wkg.Threading.Workloads.Factories;

public class ClassfulWorkloadFactory<THandle> : AbstractClassfulWorkloadFactory<THandle> where THandle : unmanaged
{
    internal protected ClassfulWorkloadFactory(IClassfulQdisc<THandle> root) : base(root)
    {
    }

    // we know that the root is classful, so we can safely do this
    public IClassfulQdisc<THandle> Root => ClassfulRoot;
}
