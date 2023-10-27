using System.Diagnostics.CodeAnalysis;
using Wkg.Extensions.Common;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing;

/// <summary>
/// Base class for qdiscs.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
public abstract class Qdisc<THandle> : IQdisc<THandle> where THandle : unmanaged
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
    protected INotifyWorkScheduled ParentScheduler => Volatile.Read(ref _parentScheduler);

    /// <inheritdoc/>
    public ref readonly THandle Handle => ref _handle;

    /// <inheritdoc/>
    public abstract bool IsEmpty { get; }

    /// <inheritdoc/>
    public abstract int Count { get; }

    internal bool IsCompleted => ReferenceEquals(ParentScheduler, NotifyWorkScheduledSentinel.Completed);

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

    /// <inheritdoc cref="IQdisc.TryDequeueInternal(bool, out Workload?)"/>"
    protected abstract bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out Workload? workload);

    /// <inheritdoc cref="IQdisc.TryRemoveInternal(Workload)"/>"
    protected abstract bool TryRemoveInternal(Workload workload);

    bool IQdisc.TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out Workload? workload) => TryDequeueInternal(backTrack, out workload);

    bool IQdisc.TryRemoveInternal(Workload workload) => TryRemoveInternal(workload);

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

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} ({Handle})";
}
