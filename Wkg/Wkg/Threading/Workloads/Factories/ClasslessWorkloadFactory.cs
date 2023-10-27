using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public class ClasslessWorkloadFactory<THandle> : AbstractClasslessWorkloadFactory<THandle> where THandle : unmanaged
{
    internal protected ClasslessWorkloadFactory(IClasslessQdisc<THandle> root) : base(root)
    {
    }

    public IClasslessQdisc<THandle> Root => _root;
}
