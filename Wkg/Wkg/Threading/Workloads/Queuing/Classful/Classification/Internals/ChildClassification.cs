using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;

internal class ChildClassification<THandle>(IClasslessQdisc<THandle> child, Predicate<object?> _predicate) : IChildClassification<THandle>
    where THandle : unmanaged
{
    public IClasslessQdisc<THandle> Qdisc { get; } = child;

    public bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // supports classification, but only if the predicate matches
        if (_predicate(state))
        {
            Qdisc.Enqueue(workload);
            return true;
        }
        return false;
    }

    public bool CanClassify(object? state) => _predicate(state);
}
