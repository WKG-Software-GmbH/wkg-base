﻿using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public interface IClassfulQdisc : IQdisc, INotifyWorkScheduled
{
}

public interface IClassfulQdisc<THandle> : IClassfulQdisc, IClasslessQdisc<THandle>
    where THandle : unmanaged
{
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

    internal bool TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child);

    internal bool ContainsChild(in THandle handle);
}
