using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;

internal class ChildClassification<THandle> : IChildClassification<THandle>
    where THandle : unmanaged
{
    private readonly Predicate<object?> _predicate;

    public IClasslessQdisc<THandle> Qdisc { get; }

    public ChildClassification(IClasslessQdisc<THandle> child, Predicate<object?> predicate)
    {
        Qdisc = child;
        _predicate = predicate;
    }

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
