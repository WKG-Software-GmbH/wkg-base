using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Internals;

internal class ClassfulChildClassification<THandle> : IChildClassification<THandle>
    where THandle : unmanaged
{
    private readonly IClassfulQdisc<THandle> _qdisc;

    public ClassfulChildClassification(IClassfulQdisc<THandle> child)
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