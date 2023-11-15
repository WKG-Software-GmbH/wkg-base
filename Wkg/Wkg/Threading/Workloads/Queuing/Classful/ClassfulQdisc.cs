using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Classful.Routing;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful;

public abstract class ClassfulQdisc<THandle> : ClasslessQdisc<THandle>, IClassfulQdisc<THandle> 
    where THandle : unmanaged
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
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClassfulQdisc<THandle> child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryEnqueue(object?, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryEnqueueDirect(object?, AbstractWorkloadBase)"/>""
    protected abstract bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.CanClassify(object?)"/>""
    protected abstract bool CanClassify(object? state);

    /// <inheritdoc/>
    public abstract bool RemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryRemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.ContainsChild(THandle)"/>"
    protected abstract bool ContainsChild(THandle handle);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryFindChild(THandle, out IClasslessQdisc{THandle}?)"/>
    protected abstract bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryFindRoute(THandle, ref RoutingPath{THandle})"/>
    protected abstract bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.WillEnqueueFromRoutingPath(ref RoutingPathNode{THandle}, AbstractWorkloadBase)"/>
    protected virtual void WillEnqueueFromRoutingPath(ref RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload) => Pass();

    bool IClassfulQdisc<THandle>.TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);

    bool IClassfulQdisc<THandle>.TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);

    bool IClassfulQdisc<THandle>.ContainsChild(THandle handle) => ContainsChild(handle);

    void INotifyWorkScheduled.OnWorkScheduled() => OnWorkScheduled();

    bool IClassfulQdisc<THandle>.TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child) =>
        TryFindChild(handle, out child);

    bool IClassfulQdisc<THandle>.CanClassify(object? state) => CanClassify(state);

    void IClassfulQdisc<THandle>.WillEnqueueFromRoutingPath(ref RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload) => 
        WillEnqueueFromRoutingPath(ref routingPathNode, workload);

    bool IClassfulQdisc<THandle>.TryFindRoute(THandle handle, ref RoutingPath<THandle> path) => TryFindRoute(handle, ref path);

    INotifyWorkScheduled IClasslessQdisc.ParentScheduler => ParentScheduler;

    void INotifyWorkScheduled.DisposeRoot() => ParentScheduler.DisposeRoot();
}