using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public abstract class WorkloadFactory<THandle> where THandle : unmanaged
{
    private protected IClasslessQdisc<THandle> _root;
    // TODO: pooling entry point

    private protected WorkloadFactory(IClasslessQdisc<THandle> root)
    {
        _root = root;
    }

    private protected ref IClasslessQdisc<THandle> RootRef => ref _root;
}
