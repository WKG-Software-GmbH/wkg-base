using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Routing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classless;

/// <summary>
/// Base class for qdiscs.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
public abstract class ClasslessQdisc<THandle> : IClassifyingQdisc<THandle> where THandle : unmanaged
{
    private readonly THandle _handle;
    private INotifyWorkScheduled _parentScheduler;
    private protected bool _disposedValue;

    protected ClasslessQdisc(THandle handle, Predicate<object?>? predicate)
    {
        _handle = handle;
        _parentScheduler = NotifyWorkScheduledSentinel.Uninitialized;
        Predicate = predicate ?? MatchNothingPredicate;
    }

    /// <summary>
    /// The predicate that determines whether a workload can be enqueued into this qdisc.
    /// </summary>
    protected Predicate<object?> Predicate { get; }

    /// <summary>
    /// The parent scheduler of this qdisc.
    /// </summary>
    private protected INotifyWorkScheduled ParentScheduler => Volatile.Read(ref _parentScheduler);

    /// <inheritdoc/>
    public ref readonly THandle Handle => ref _handle;

    /// <inheritdoc/>
    public abstract bool IsEmpty { get; }

    /// <inheritdoc/>
    public abstract int Count { get; }

    bool IClassifyingQdisc.IsCompleted => ReferenceEquals(ParentScheduler, NotifyWorkScheduledSentinel.Completed);

    void IClassifyingQdisc.AssertNotCompleted()
    {
        if (this.To<IClassifyingQdisc>().IsCompleted)
        {
            ThrowCompleted();
        }
    }

    [DoesNotReturn]
    private void ThrowCompleted()
    {
        ObjectDisposedException exception = new(ToString(), "This qdisc was already marked as completed and is no longer accepting new workloads.");
        ExceptionDispatchInfo.SetCurrentStackTrace(exception);
        DebugLog.WriteException(exception, LogWriter.Blocking);
        throw exception;
    }

    /// <summary>
    /// Called after this qdisc has been bound to a parent scheduler.
    /// </summary>
    /// <param name="parentScheduler">The parent scheduler.</param>
    protected virtual void OnInternalInitialize(INotifyWorkScheduled parentScheduler) => Pass();

    void IQdisc.InternalInitialize(INotifyWorkScheduled parentScheduler)
    {
        DebugLog.WriteDiagnostic($"Initializing qdisc {this} with parent scheduler {parentScheduler}.", LogWriter.Blocking);
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _parentScheduler, parentScheduler, NotifyWorkScheduledSentinel.Uninitialized), NotifyWorkScheduledSentinel.Uninitialized))
        {
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual("A workload scheduler was already set for this qdisc. This is a bug in the qdisc implementation.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            throw exception;
        }
        OnInternalInitialize(parentScheduler);
    }

    /// <inheritdoc cref="IQdisc.TryDequeueInternal(int, bool, out AbstractWorkloadBase?)"/>"
    protected abstract bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    /// <inheritdoc cref="IQdisc.TryPeekUnsafe(int, out AbstractWorkloadBase?)"/>"
    protected abstract bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    /// <inheritdoc cref="IQdisc.TryRemoveInternal(AwaitableWorkload)"/>"
    protected abstract bool TryRemoveInternal(AwaitableWorkload workload);

    /// <inheritdoc cref="IClassifyingQdisc.Enqueue(AbstractWorkloadBase)"/>"
    protected abstract void EnqueueDirect(AbstractWorkloadBase workload);

    /// <summary>
    /// Attempts to bind the specified workload to this qdisc.
    /// </summary>
    /// <remarks>
    /// The workload should be bound first, before being enqueued into the qdisc. Only bind the workload if this qdisc actually stores the workload itself, i.e., if this qdisc does not delegate to another qdisc.
    /// </remarks>
    /// <param name="workload">The workload to bind.</param>
    /// <returns><see langword="true"/> if the workload was successfully bound to the qdisc; <see langword="false"/> if the workload has already completed, is in an unbindable state, or another binding operation was faster.</returns>
    protected bool TryBindWorkload(AbstractWorkloadBase workload) => workload.TryInternalBindQdisc(this);

    /// <summary>
    /// Notifies the parent scheduler that there is a new workload available for processing.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: be sure to call base.NotifyWorkScheduled() in derived classes. Otherwise, the parent scheduler will not be notified of the scheduled workload.<br/>
    /// <see langword="WARNING"/>: only call this method in qdiscs that do not delegate the enqueue operation to another qdisc. Otherwise, the parent scheduler will be notified multiple times for the same workload.<br/>
    /// <see langword="WARNING"/>: ensure that this method is only called when the workload has successfully enqueued and was bound to this qdisc. Otherwise, the parent scheduler will be notified of a workload that is not actually available for processing.
    /// </remarks>
    protected void NotifyWorkScheduled() => ParentScheduler.OnWorkScheduled();

    /// <inheritdoc cref="IQdisc.OnWorkerTerminated(int)"/>
    protected virtual void OnWorkerTerminated(int workerId) => Pass();

    bool IQdisc.TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => TryDequeueInternal(workerId, backTrack, out workload);

    bool IQdisc.TryRemoveInternal(AwaitableWorkload workload) => TryRemoveInternal(workload);

    void IQdisc.Complete()
    {
        DebugLog.WriteDiagnostic($"Marking qdisc {GetType().Name} ({Handle}) as completed. Future scheduling attempts will be rejected.", LogWriter.Blocking);
        if (ReferenceEquals(Interlocked.Exchange(ref _parentScheduler, NotifyWorkScheduledSentinel.Completed), NotifyWorkScheduledSentinel.Uninitialized))
        {
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual("This qdisc was already completed. This is a bug in the qdisc implementation.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            throw exception;
        }
    }

    void IClassifyingQdisc.Enqueue(AbstractWorkloadBase workload) => EnqueueDirect(workload);

    INotifyWorkScheduled IClassifyingQdisc.ParentScheduler => ParentScheduler;

    void IQdisc.OnWorkerTerminated(int workerId) => OnWorkerTerminated(workerId);

    bool IQdisc.TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => TryPeekUnsafe(workerId, out workload);

    #region IClassifyingQdisc / IClassifyingQdisc<THandle> implementation

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.TryEnqueueByHandle(THandle, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.WillEnqueueFromRoutingPath(ref readonly RoutingPathNode{THandle}, AbstractWorkloadBase)"/>"
    protected virtual void WillEnqueueFromRoutingPath(ref readonly RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload) => Pass();

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.TryFindRoute(THandle, ref RoutingPath{THandle})"/>"
    protected abstract bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path);

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.ContainsChild(THandle)"/>"
    protected abstract bool ContainsChild(THandle handle);

    /// <inheritdoc cref="IClassifyingQdisc.CanClassify(object?)"/>"
    protected abstract bool CanClassify(object? state);

    /// <inheritdoc cref="IClassifyingQdisc.TryEnqueue(object?, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassifyingQdisc.TryEnqueueDirect(object?, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    bool IClassifyingQdisc<THandle>.TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload) => TryEnqueueByHandle(handle, workload);
    void IClassifyingQdisc<THandle>.WillEnqueueFromRoutingPath(ref readonly RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload) => WillEnqueueFromRoutingPath(in routingPathNode, workload);
    bool IClassifyingQdisc<THandle>.TryFindRoute(THandle handle, ref RoutingPath<THandle> path) => TryFindRoute(handle, ref path);
    bool IClassifyingQdisc<THandle>.ContainsChild(THandle handle) => ContainsChild(handle);
    bool IClassifyingQdisc.CanClassify(object? state) => CanClassify(state);
    bool IClassifyingQdisc.TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);
    bool IClassifyingQdisc.TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);

    #endregion IClassifyingQdisc / IClassifyingQdisc<THandle> implementation

    /// <summary>
    /// A predicate that matches everything.
    /// </summary>
    /// <returns><see langword="true"/></returns>
    protected static bool MatchEverythingPredicate(object? _) => true;

    /// <summary>
    /// A predicate that matches nothing.
    /// </summary>
    /// <returns><see langword="false"/></returns>
    protected static bool MatchNothingPredicate(object? _) => false;

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} ({Handle})";

    /// <summary>
    /// Disposes of any managed resources held by this qdisc.
    /// </summary>
    protected virtual void DisposeManaged() => Pass();

    /// <summary>
    /// Disposes of any unmanaged resources held by this qdisc.
    /// </summary>
    protected virtual void DisposeUnmanaged() => Pass();

    /// <summary>
    /// Disposes of any resources held by this qdisc.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if this method is called from <see cref="Dispose()"/>; <see langword="false"/> if this method is called from the finalizer.</param>
    protected void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                DisposeManaged();
            }

            DisposeUnmanaged();
            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
