using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers.Internals;

internal class ChildClassification<THandle, TState> : IChildClassification<THandle>
    where THandle : unmanaged
    where TState : class
{
    private readonly Predicate<TState> _predicate;

    public IClasslessQdisc<THandle> Qdisc { get; }

    public ChildClassification(IClasslessQdisc<THandle> child, Predicate<TState> predicate)
    {
        Qdisc = child;
        _predicate = predicate;
    }

    public bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // supports classification, but only if the predicate matches
        if (state is TState typedState && _predicate(typedState))
        {
            Qdisc.Enqueue(workload);
            return true;
        }
        return false;
    }
}
