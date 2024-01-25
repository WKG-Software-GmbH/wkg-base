using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public abstract class ClassfulQdisc<THandle> : ClasslessQdisc<THandle>, IClassfulQdisc<THandle> 
    where THandle : unmanaged
{
    protected ClassfulQdisc(THandle handle, Predicate<object?>? predicate) : base(handle, predicate)
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
    public abstract bool RemoveChild(IClassifyingQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClassifyingQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryRemoveChild(IClassifyingQdisc<THandle> child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryFindChild(THandle, out IClassifyingQdisc{THandle}?)"/>
    protected abstract bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child);

    void INotifyWorkScheduled.OnWorkScheduled() => OnWorkScheduled();

    bool IClassfulQdisc<THandle>.TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child) =>
        TryFindChild(handle, out child);

    INotifyWorkScheduled IClassifyingQdisc.ParentScheduler => ParentScheduler;

    void INotifyWorkScheduled.DisposeRoot() => ParentScheduler.DisposeRoot();
}