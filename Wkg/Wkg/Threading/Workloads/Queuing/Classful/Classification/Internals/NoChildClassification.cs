using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;

internal class NoChildClassification<THandle>(IClasslessQdisc<THandle> child) : IChildClassification<THandle>
    where THandle : unmanaged
{
    public IClasslessQdisc<THandle> Qdisc { get; } = child;

    // does not support classification, always returns false
    public bool TryEnqueue(object? state, AbstractWorkloadBase workload) => false;

    public bool CanClassify(object? state) => false;
}
