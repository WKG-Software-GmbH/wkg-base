using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public abstract class ClassfulQdisc<THandle, TState> : ClasslessQdisc<THandle>, IClassfulQdisc<THandle, TState>
    where THandle : unmanaged
    where TState : class
{
    protected ClassfulQdisc(THandle handle) : base(handle)
    {
    }

    /// <summary>
    /// Called when a workload is scheduled to any child qdisc.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: be sure to call base.OnWorkScheduled() in derived classes. Otherwise, the parent scheduler will not be notified of the scheduled workload.
    /// </remarks>
    protected virtual void OnWorkScheduled() => ParentScheduler.OnWorkScheduled();

    /// <summary>
    /// Binds the specified child qdisc to this qdisc, allowing child notifications to be propagated to the parent scheduler.
    /// </summary>
    /// <param name="child">The child qdisc to bind.</param>
    protected void BindChildQdisc(IQdisc child) =>
        child.InternalInitialize(this);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate);

    /// <inheritdoc/>
    public abstract bool TryAddChild<TOtherState>(IClassfulQdisc<THandle, TOtherState> child) where TOtherState : class;

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryEnqueue(object?, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryEnqueueDirect(object?, AbstractWorkloadBase)"/>""
    protected abstract bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc/>
    public abstract bool RemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryRemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.ContainsChild(in THandle)"/>"
    protected abstract bool ContainsChild(in THandle handle);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryFindChild(in THandle, out IClasslessQdisc{THandle}?)"/>
    protected abstract bool TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child);

    bool IClassfulQdisc<THandle>.TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);

    bool IClassfulQdisc<THandle>.TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);

    bool IClassfulQdisc<THandle>.ContainsChild(in THandle handle) => ContainsChild(in handle);

    void INotifyWorkScheduled.OnWorkScheduled() => OnWorkScheduled();

    bool IClassfulQdisc<THandle>.TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child) =>
        TryFindChild(in handle, out child);
}