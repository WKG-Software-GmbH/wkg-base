using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Internals;

internal class ClassifierChildClassification<THandle, TState> : IChildClassification<THandle>
    where THandle : unmanaged
    where TState : class
{
    private readonly IClassfulQdisc<THandle, TState> _qdisc;

    public ClassifierChildClassification(IClassfulQdisc<THandle, TState> child)
    {
        _qdisc = child;
    }

    public IClasslessQdisc<THandle> Qdisc => _qdisc;

    public bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification first, then try parent classification
        if (_qdisc.TryEnqueue(state, workload))
        {
            return true;
        }
        // supports classification, but only if the predicate matches
        return _qdisc.TryEnqueueDirect(state, workload);
    }
}