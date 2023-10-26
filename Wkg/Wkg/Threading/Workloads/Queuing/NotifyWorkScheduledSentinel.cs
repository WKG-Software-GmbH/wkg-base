using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing;

public class NotifyWorkScheduledSentinel : INotifyWorkScheduled
{
    private NotifyWorkScheduledSentinel() => Pass();

    public static readonly NotifyWorkScheduledSentinel Uninitialized = new();

    public static readonly NotifyWorkScheduledSentinel Completed = new();

    [DoesNotReturn]
    void INotifyWorkScheduled.OnWorkScheduled()
    {
        WorkloadSchedulingException exception = new("A workload was scheduled, but no workload scheduler was found in the current context. This is a bug in the qdisc implementation.");
        DebugLog.WriteException(exception, LogWriter.Blocking);
        throw exception;
    }
}
