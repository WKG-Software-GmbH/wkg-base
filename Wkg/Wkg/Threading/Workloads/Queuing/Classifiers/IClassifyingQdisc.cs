using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers;

public interface IClassifyingQdisc<THandle> : IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    internal bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    internal bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);
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
    /// <summary>
    /// Creates a new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/> and <paramref name="predicate"/>.
    /// </summary>
    /// <param name="handle">The handle uniquely identifying this qdisc. The handle must not be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c> and must not be used by any other qdisc.</param>
    /// <param name="predicate">The predicate used to determine whether a workload should be enqueued onto this qdisc. Child qdiscs should be considered separately.</param>
    /// <returns>A new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/> and <paramref name="predicate"/>.</returns>
    static abstract TQdisc Create(THandle handle, Predicate<TState> predicate);

    /// <summary>
    /// Creates a new anonymous <typeparamref name="TQdisc"/> instance with the specified <paramref name="predicate"/>. The handle is not used for classification and may be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c>.
    /// </summary>
    /// <param name="predicate">The predicate used to determine whether a workload should be enqueued onto this qdisc. Child qdiscs should be considered separately.</param>
    /// <returns>A new anonymous <typeparamref name="TQdisc"/> instance with the specified <paramref name="predicate"/>.</returns>
    static abstract TQdisc CreateAnonymous(Predicate<TState> predicate);
}