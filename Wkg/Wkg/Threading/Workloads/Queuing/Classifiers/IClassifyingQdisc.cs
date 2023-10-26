using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers;

public interface IClassifyingQdisc<THandle> : IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    bool TryEnqueue(object? state, Workload workload);

    bool TryEnqueueDirect(object? state, Workload workload);
}

public interface IClassifyingQdisc<THandle, TState> : IClassifyingQdisc<THandle>
    where THandle : unmanaged
    where TState : class
{
    bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate);

    bool TryAddChild<TOtherState>(IClassifyingQdisc<THandle, TOtherState> child) where TOtherState : class;
}