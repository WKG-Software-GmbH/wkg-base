﻿using Wkg.Internals.Diagnostic;
using Wkg.Threading.Workloads.Exceptions;

namespace Wkg.Threading.Workloads.Queuing.Classless;

/// <summary>
/// A leaf qdisc with no child-management capabilities.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ClasslessQdisc{THandle}"/> class.
/// </remarks>
/// <param name="handle">The handle of the qdisc.</param>
/// <param name="predicate">The predicate used to determine if a workload can be scheduled.</param>
public abstract class ClasslessQdisc<THandle>(THandle handle, Predicate<object?>? predicate) 
    : ClassifyingQdisc<THandle>(handle, predicate) where THandle : unmanaged
{

    /// <summary>
    /// Enqueues the <paramref name="workload"/> onto the local queue, without additional checks or setup.
    /// </summary>
    /// <param name="workload">The already bound workload to enqueue.</param>
    protected abstract void EnqueueDirectLocal(AbstractWorkloadBase workload);

    /// <inheritdoc/>
    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        if (TryBindWorkload(workload))
        {
            EnqueueDirectLocal(workload);
            NotifyWorkScheduled();
        }
        else if (workload.IsCompleted)
        {
            DebugLog.WriteInfo(SR.ThreadingWorkloads_QdiscEnqueueFailed_AlreadyCompleted);
        }
        else
        {
            throw new WorkloadSchedulingException(SR.ThreadingWorkloads_QdiscEnqueueFailed_NotBound);
        }
    }
}
