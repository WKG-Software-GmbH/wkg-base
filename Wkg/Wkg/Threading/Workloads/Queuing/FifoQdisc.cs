using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing;

internal class FifoQdisc : IClasslessQdisc
{
    private readonly ConcurrentQueue<Workload> _queue = new();

    private INotifyWorkScheduled _parentScheduler = NotifyWorkScheduledSentinel.Instance;

    public void InternalInitialize(INotifyWorkScheduled parentScheduler)
    {
        DebugLog.WriteDiagnostic($"Initializing qdisc with parent scheduler {parentScheduler}.", LogWriter.Blocking);
        if (Interlocked.CompareExchange(ref _parentScheduler, parentScheduler, NotifyWorkScheduledSentinel.Instance) != NotifyWorkScheduledSentinel.Instance)
        {
            WorkloadSchedulingException exception = new("A workload scheduler was already set for this qdisc. This is a bug in the qdisc implementation.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            throw exception;
        }
    }

    public bool IsEmpty => _queue.IsEmpty;

    public void Enqueue(Workload workload)
    {
        INotifyWorkScheduled parentScheduler = Volatile.Read(ref _parentScheduler);
        _queue.Enqueue(workload);
        if (workload.TryInternalBindQdisc(this))
        {
            parentScheduler.OnWorkScheduled();
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    public bool TryDequeue(bool backTrack, [NotNullWhen(true)] out Workload? workload) => _queue.TryDequeue(out workload);

    public bool TryRemove(Workload workload) => false;
}
