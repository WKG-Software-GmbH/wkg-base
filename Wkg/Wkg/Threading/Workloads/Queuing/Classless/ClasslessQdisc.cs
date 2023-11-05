using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classless;

/// <summary>
/// Base class for qdiscs.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
public abstract class ClasslessQdisc<THandle> : IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly THandle _handle;
    private INotifyWorkScheduled _parentScheduler;

    protected ClasslessQdisc(THandle handle)
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

    /// <inheritdoc cref="IQdisc.TryDequeueInternal(int, bool, out AbstractWorkloadBase?)"/>"
    protected abstract bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    /// <inheritdoc cref="IQdisc.TryRemoveInternal(AwaitableWorkload)"/>"
    protected abstract bool TryRemoveInternal(AwaitableWorkload workload);

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

    /// <inheritdoc cref="IQdisc.OnWorkerTerminated(int)"/>
    protected virtual void OnWorkerTerminated(int workerId) => Pass();

    bool IQdisc.TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => TryDequeueInternal(workerId, backTrack, out workload);

    bool IQdisc.TryRemoveInternal(AwaitableWorkload workload) => TryRemoveInternal(workload);

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

    void IQdisc.OnWorkerTerminated(int workerId) => OnWorkerTerminated(workerId);

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} ({Handle})";
}
