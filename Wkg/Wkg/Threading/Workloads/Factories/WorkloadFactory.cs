using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class WorkloadFactory<THandle> where THandle : unmanaged
{
    private protected IClasslessQdisc<THandle> _root;

    private protected AnonymousWorkloadPoolManager? Pool { get; }

    public WorkloadContextOptions DefaultOptions { get; }

    private protected WorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options)
    {
        _root = root;
        Pool = pool;
        DefaultOptions = options ?? new WorkloadContextOptions();
    }

    [MemberNotNullWhen(true, nameof(Pool))]
    private protected bool SupportsPooling => Pool is not null;

    private protected ref IClasslessQdisc<THandle> RootRef => ref _root;
}
