using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing;

/// <summary>
/// Base class for qdiscs.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
public abstract class Qdisc<THandle> : IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly THandle _handle;
    private INotifyWorkScheduled _parentScheduler;

    protected Qdisc(THandle handle)
    {
        _handle = handle;
        _parentScheduler = NotifyWorkScheduledSentinel.Uninitialized;
    }

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

    internal bool IsCompleted => ReferenceEquals(ParentScheduler, NotifyWorkScheduledSentinel.Completed);

    /// <summary>
    /// Called after this qdisc has been bound to a parent scheduler.
    /// </summary>
    /// <param name="parentScheduler">The parent scheduler.</param>
    protected virtual void OnInternalInitialize(INotifyWorkScheduled parentScheduler) => Pass();

    void IQdisc.InternalInitialize(INotifyWorkScheduled parentScheduler)
    {
        DebugLog.WriteDiagnostic($"Initializing qdisc {GetType().Name} ({Handle}) with parent scheduler {parentScheduler}.", LogWriter.Blocking);
        if (!ReferenceEquals(Interlocked.CompareExchange(ref _parentScheduler, parentScheduler, NotifyWorkScheduledSentinel.Uninitialized), NotifyWorkScheduledSentinel.Uninitialized))
        {
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual("A workload scheduler was already set for this qdisc. This is a bug in the qdisc implementation.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            throw exception;
        }
        OnInternalInitialize(parentScheduler);
    }

    /// <inheritdoc cref="IQdisc.TryDequeueInternal(bool, out AbstractWorkloadBase?)"/>"
    protected abstract bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    /// <inheritdoc cref="IQdisc.TryRemoveInternal(CancelableWorkload)"/>"
    protected abstract bool TryRemoveInternal(CancelableWorkload workload);

    /// <inheritdoc cref="IClasslessQdisc.Enqueue(AbstractWorkloadBase)"/>"
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

    bool IQdisc.TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => TryDequeueInternal(backTrack, out workload);

    bool IQdisc.TryRemoveInternal(CancelableWorkload workload) => TryRemoveInternal(workload);

    void IQdisc.Complete()
    {
        DebugLog.WriteDiagnostic($"Completing qdisc {GetType().Name} ({Handle}).", LogWriter.Blocking);
        if (ReferenceEquals(Interlocked.Exchange(ref _parentScheduler, NotifyWorkScheduledSentinel.Completed), NotifyWorkScheduledSentinel.Uninitialized))
        {
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual("This qdisc was already completed. This is a bug in the qdisc implementation.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            throw exception;
        }
    }

    void IClasslessQdisc.Enqueue(AbstractWorkloadBase workload) => EnqueueDirect(workload);

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} ({Handle})";
}

public abstract class ClassfulQdisc<THandle> : Qdisc<THandle>, IClassfulQdisc<THandle> where THandle : unmanaged
{
    protected ClassfulQdisc(THandle handle) : base(handle)
    {
    }

    /// <inheritdoc/>
    public abstract bool RemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc/>
    public abstract bool TryRemoveChild(IClasslessQdisc<THandle> child);

    /// <inheritdoc cref="IClassfulQdisc{THandle}.ContainsChild(in THandle)"/>"
    protected abstract bool ContainsChild(in THandle handle);

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

    /// <inheritdoc cref="IClassfulQdisc{THandle}.TryFindChild(in THandle, out IClasslessQdisc{THandle}?)"/>
    protected abstract bool TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child);

    bool IClassfulQdisc<THandle>.ContainsChild(in THandle handle) => ContainsChild(in handle);

    void INotifyWorkScheduled.OnWorkScheduled() => OnWorkScheduled();

    bool IClassfulQdisc<THandle>.TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child) => 
        TryFindChild(in handle, out child);
}

public abstract class ClassifyingQdisc<THandle, TState> : ClassfulQdisc<THandle>, IClassifyingQdisc<THandle, TState>
    where THandle : unmanaged
    where TState : class
{
    protected ClassifyingQdisc(THandle handle) : base(handle)
    {
    }

    /// <inheritdoc/>
    public abstract bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate);

    /// <inheritdoc/>
    public abstract bool TryAddChild<TOtherState>(IClassifyingQdisc<THandle, TOtherState> child) where TOtherState : class;

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.TryEnqueue(object?, AbstractWorkloadBase)"/>"
    protected abstract bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    /// <inheritdoc cref="IClassifyingQdisc{THandle}.TryEnqueueDirect(object?, AbstractWorkloadBase)"/>""
    protected abstract bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    bool IClassifyingQdisc<THandle>.TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);

    bool IClassifyingQdisc<THandle>.TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);
}