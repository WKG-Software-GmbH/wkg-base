using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public abstract class WorkloadFactory<THandle> where THandle : unmanaged
{
    private protected IClasslessQdisc<THandle> _root;
    private protected AnonymousWorkloadPool? Pool { get; }

    private protected WorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPool? pool)
    {
        _root = root;
        Pool = pool;
    }

    [MemberNotNullWhen(true, nameof(Pool))]
    private protected bool SupportsPooling => Pool is not null;

    private protected ref IClasslessQdisc<THandle> RootRef => ref _root;
}
