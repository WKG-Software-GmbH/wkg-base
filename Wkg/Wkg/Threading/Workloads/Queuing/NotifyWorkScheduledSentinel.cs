using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Exceptions;

namespace Wkg.Threading.Workloads.Queuing;

/// <summary>
/// Represents a sentinel implementation of <see cref="INotifyWorkScheduled"/> that throws an exception when a workload is scheduled.
/// </summary>
/// <remarks>
/// This implementation is used to prevent workloads from being scheduled on partially-initialized qdiscs.
/// </remarks>
public class NotifyWorkScheduledSentinel : INotifyWorkScheduled
{
    private NotifyWorkScheduledSentinel() => Pass();

    /// <summary>
    /// A sentinal instance representing an uninitialized qdisc.
    /// </summary>
    public static readonly NotifyWorkScheduledSentinel Uninitialized = new();

    /// <summary>
    /// A sentinal instance representing a completed qdisc.
    /// </summary>
    public static readonly NotifyWorkScheduledSentinel Completed = new();

    [DoesNotReturn]
    void INotifyWorkScheduled.OnWorkScheduled()
    {
        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual(SR.ThreadingWorkloads_QdiscEnqueueFailed_NoScheduler);
        DebugLog.WriteException(exception, LogWriter.Blocking);
        throw exception;
    }

    void INotifyWorkScheduled.DisposeRoot() => Pass();
}
