using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers;

public interface IClassifyingQdisc<THandle> : IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);
}

public interface IClassifyingQdisc<THandle, TState> : IClassifyingQdisc<THandle>
    where THandle : unmanaged
    where TState : class
{
    bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate);

    bool TryAddChild<TOtherState>(IClassifyingQdisc<THandle, TOtherState> child) where TOtherState : class;
}

public interface IClassifyingQdisc<THandle, TState, TQdisc> : IClassifyingQdisc<THandle, TState>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassifyingQdisc<THandle, TState, TQdisc>
{
    static abstract TQdisc Create(THandle handle, Predicate<TState> predicate);
}