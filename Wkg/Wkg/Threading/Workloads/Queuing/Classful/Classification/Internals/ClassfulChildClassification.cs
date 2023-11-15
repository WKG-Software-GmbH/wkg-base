using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;

internal class ClassfulChildClassification<THandle>(IClassfulQdisc<THandle> _child) : IChildClassification<THandle>
    where THandle : unmanaged
{
    public IClasslessQdisc<THandle> Qdisc => _child;

    public bool CanClassify(object? state) => _child.CanClassify(state);

    public bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification first, then try parent classification
        if (_child.TryEnqueue(state, workload))
        {
            return true;
        }
        // supports classification, but only if the predicate matches
        return _child.TryEnqueueDirect(state, workload);
    }
}