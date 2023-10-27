using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers.Internals;

internal class NoChildClassification<THandle> : IChildClassification<THandle>
    where THandle : unmanaged
{
    public IClasslessQdisc<THandle> Qdisc { get; }

    public NoChildClassification(IClasslessQdisc<THandle> child)
    {
        Qdisc = child;
    }

    // does not support classification, always returns false
    public bool TryEnqueue(object? state, AbstractWorkloadBase workload) => false;
}
