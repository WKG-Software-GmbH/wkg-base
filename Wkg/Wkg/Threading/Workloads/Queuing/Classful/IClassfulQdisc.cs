using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public interface IClassfulQdisc<THandle> : IQdisc, INotifyWorkScheduled, IClassifyingQdisc<THandle>
    where THandle : unmanaged
{
    /// <summary>
    /// Attempts to add the child to the qdisc.
    /// </summary>
    /// <param name="child">The child to add.</param>
    /// <returns><see langword="true"/> if the child was added, <see langword="false"/> if the child was already added.</returns>
    bool TryAddChild(IClassifyingQdisc<THandle> child);

    /// <summary>
    /// Attempts to remove the child from the qdisc.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    /// <returns><see langword="true"/> if the child was removed, <see langword="false"/> if the child was not found or in use.</returns>
    bool TryRemoveChild(IClassifyingQdisc<THandle> child);

    /// <summary>
    /// Removes the child from the qdisc, blocking until the child is no longer in use if necessary.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    /// <returns><see langword="true"/> if the child was removed, <see langword="false"/> if the child was not found.</returns>
    bool RemoveChild(IClassifyingQdisc<THandle> child);

    /// <summary>
    /// Attempts to find the child with the given handle.
    /// </summary>
    /// <param name="handle">The handle of the child to find.</param>
    /// <param name="child">The child with the given handle, if found.</param>
    /// <returns><see langword="true"/> if the child was found, <see langword="false"/> if the child was not found.</returns>
    /// <remarks>
    /// Classless qdiscs are not required to expose their children, so this method may return <see langword="false"/> even if <see cref="IClassifyingQdisc{THandle}.ContainsChild(THandle)"/> returns <see langword="true"/>.<br/>
    /// Classful qdiscs are required to expose their direct children.<br/>
    /// Qdiscs should only check their children, not their own handle. Checking the qdisc's own handle is the responsibility of the caller. This optimization avoids unnecessary recursion.
    /// </remarks>
    internal bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child);
}