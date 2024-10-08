﻿using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.LatestOnly;

internal class LatestOnlyQdisc<THandle>(THandle handle, Predicate<object?>? predicate) : ClassifyingQdisc<THandle>(handle, predicate) where THandle : unmanaged
{
    private volatile AbstractWorkloadBase? _singleWorkload;

    public override bool IsEmpty => _singleWorkload is null;

    public override int BestEffortCount => IsEmpty ? 0 : 1;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        // TODO: parent qdiscs may buffer workloads. That is not within our control.
        // they honestly shouldn't, but we can't guarantee that they won't (e.g., GfqQdisc)
        if (TryBindWorkload(workload))
        {
            AbstractWorkloadBase? old = Interlocked.Exchange(ref _singleWorkload, workload);
            if (old is null)
            {
                // we only need to notify the scheduler if we have a new workload
                // otherwise, as far as the scheduler is concerned, nothing has changed
                NotifyWorkScheduled();
            }
            else
            {
                // we need to abort the old workload and invoke any continuations
                old?.InternalAbort();
            }
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

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        workload = Interlocked.Exchange(ref _singleWorkload, null);
        return workload is not null;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        if (Predicate.Invoke(state))
        {
            EnqueueDirect(workload);
            return true;
        }
        return false;
    }

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload) => false;

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload) => TryEnqueue(state, workload);

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path) => false;

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        workload = _singleWorkload;
        return workload is not null;
    }

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => Interlocked.CompareExchange(ref _singleWorkload, null, workload) is not null;
}
