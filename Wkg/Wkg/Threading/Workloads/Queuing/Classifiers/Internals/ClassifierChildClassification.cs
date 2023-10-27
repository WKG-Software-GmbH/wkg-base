using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers.Internals;

internal class ClassifierChildClassification<THandle, TState> : IChildClassification<THandle>
    where THandle : unmanaged
    where TState : class
{
    private readonly IClassifyingQdisc<THandle, TState> _qdisc;

    public ClassifierChildClassification(IClassifyingQdisc<THandle, TState> child)
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