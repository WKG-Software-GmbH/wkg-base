using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public class ClasslessWorkloadFactory<THandle> : AbstractClasslessWorkloadFactory<THandle> where THandle : unmanaged
{
    internal ClasslessWorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPool? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public IClasslessQdisc<THandle> Root => _root;
}
