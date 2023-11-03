using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public interface IClassfulQdisc<THandle> : IQdisc, INotifyWorkScheduled, IClasslessQdisc<THandle>
    where THandle : unmanaged
{
    /// <summary>
    /// Attempts to add the child to the qdisc.
    /// </summary>
    /// <param name="child">The child to add.</param>
    /// <returns><see langword="true"/> if the child was added, <see langword="false"/> if the child was already added.</returns>
    bool TryAddChild(IClasslessQdisc<THandle> child);

    /// <summary>
    /// Attempts to remove the child from the qdisc.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    /// <returns><see langword="true"/> if the child was removed, <see langword="false"/> if the child was not found or in use.</returns>
    bool TryRemoveChild(IClasslessQdisc<THandle> child);

    /// <summary>
    /// Removes the child from the qdisc, blocking until the child is no longer in use if necessary.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    /// <returns><see langword="true"/> if the child was removed, <see langword="false"/> if the child was not found.</returns>
    bool RemoveChild(IClasslessQdisc<THandle> child);

    /// <summary>
    /// Attempts to find the child with the given handle.
    /// </summary>
    /// <param name="handle">The handle of the child to find.</param>
    /// <param name="child">The child with the given handle, if found.</param>
    /// <returns><see langword="true"/> if the child was found, <see langword="false"/> if the child was not found.</returns>
    internal bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child);

    /// <summary>
    /// Checks if the qdisc contains the child with the given handle.
    /// </summary>
    /// <param name="handle">The handle of the child to find.</param>
    /// <returns><see langword="true"/> if the child was found, <see langword="false"/> if the child was not found.</returns>
    internal bool ContainsChild(THandle handle);

    internal bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    internal bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate);

    bool TryAddChild(IClassfulQdisc<THandle> child);
}

public interface IClassfulQdisc<THandle, TQdisc> : IClassfulQdisc<THandle>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
{
    /// <summary>
    /// Creates a new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/> and <paramref name="predicate"/>.
    /// </summary>
    /// <param name="handle">The handle uniquely identifying this qdisc. The handle must not be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c> and must not be used by any other qdisc.</param>
    /// <param name="predicate">The predicate used to determine whether a workload should be enqueued onto this qdisc. Child qdiscs should be considered separately.</param>
    /// <returns>A new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/> and <paramref name="predicate"/>.</returns>
    static abstract TQdisc Create(THandle handle, Predicate<object?> predicate);

    /// <summary>
    /// Creates a new anonymous <typeparamref name="TQdisc"/> instance with the specified <paramref name="predicate"/>. The handle is not used for classification and may be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c>.
    /// </summary>
    /// <param name="predicate">The predicate used to determine whether a workload should be enqueued onto this qdisc. Child qdiscs should be considered separately.</param>
    /// <returns>A new anonymous <typeparamref name="TQdisc"/> instance with the specified <paramref name="predicate"/>.</returns>
    static abstract TQdisc CreateAnonymous(Predicate<object?> predicate);
}