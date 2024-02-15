using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements a simple priority scheduling algorithm to dequeue workloads from its children.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class PrioFastLockingBitmapQdisc<THandle> : ClassfulQdisc<THandle>, IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    private readonly object _syncRoot = new();
    private readonly BitArray _dataMap;
    private readonly IQdisc?[] _localLasts;
    private readonly IClassifyingQdisc<THandle> _localQueue;
    private readonly IClassifyingQdisc<THandle>[] _children;

    public PrioFastLockingBitmapQdisc(THandle handle, Predicate<object?>? predicate, IClasslessQdiscBuilder localQueueBuilder, IClassifyingQdisc<THandle>[] children, int maxConcurrency) : base(handle, predicate)
    {
        _localQueue = localQueueBuilder.BuildUnsafe(default(THandle), MatchNothingPredicate);
        _localLasts = new IQdisc[maxConcurrency];
        foreach (IClassifyingQdisc<THandle> child in children)
        {
            BindChildQdisc(child);
        }
        _children = [_localQueue, .. children];
        _dataMap = new BitArray(_children.Length);
    }

    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    public override bool IsEmpty
    {
        get
        {
            lock (_syncRoot)
            {
                return !_dataMap.HasAnySet();
            }
        }
    }

    public override int BestEffortCount
    {
        get
        {
            lock (_syncRoot)
            {
                if (IsEmpty)
                {
                    return 0;
                }
                // get a local snapshot of the children array, other threads may still add new children which we don't care about here
                IClassifyingQdisc<THandle>[] children = _children;
                int count = 0;
                for (int i = 0; i < children.Length; i++)
                {
                    count += children[i].BestEffortCount;
                }
                return count;
            }
        }
    }

    // not supported.
    // would only need to consider the local queue, since this
    // method is only called on the direct parent of a workload.
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // if we have to backtrack, we can do so by dequeuing from the last child qdisc
        // that was dequeued from. If the last child qdisc is empty, we can't backtrack and continue
        // with the next child qdisc.
        if (backTrack && _localLasts[workerId]?.TryDequeueInternal(workerId, backTrack, out workload) is true)
        {
            DebugLog.WriteDiagnostic($"{this} Backtracking to last child qdisc {_localLasts[workerId]!.GetType().Name} ({_localLasts[workerId]}).", LogWriter.Blocking);
            return true;
        }
        // backtracking failed, or was not requested. We need to iterate over all child qdiscs.
        lock (_syncRoot)
        {
            for (int index = 0; index < _children.Length; index++)
            {
                if (_dataMap[index] is false)
                {
                    // this child qdisc is empty, skip it
                    continue;
                }
                if (_children[index].TryDequeueInternal(workerId, backTrack, out workload))
                {
                    DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {_children[index]}.", LogWriter.Blocking);
                    // we found a workload, update the last child qdisc and reset the empty counter
                    _localLasts[workerId] = _children[index];
                    return true;
                }
                // this child qdisc is empty, mark it as such
                _dataMap[index] = false;
            }
        }
        // all children are empty
        DebugLog.WriteDebug($"{this}: All children are empty.", LogWriter.Blocking);
        workload = null;
        return false;
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        lock (_syncRoot)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (_dataMap[i] is false)
                {
                    // this child qdisc is empty, skip it
                    continue;
                }
                if (_children[i].TryPeekUnsafe(workerId, out workload))
                {
                    return true;
                }
                // this child qdisc is empty, mark it as such
                _dataMap[i] = false;
            }
        }
        workload = null;
        return false;
    }

    protected override bool CanClassify(object? state)
    {
        if (Predicate.Invoke(state))
        {
            // fast path, we can enqueue directly to the local queue
            return true;
        }

        for (int i = 0; i < _children.Length; i++)
        {
            if (_children[i].CanClassify(state))
            {
                return true;
            }
        }
        return false;
    }

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload)
    {
        DebugLog.WriteDiagnostic($"{this} Trying to enqueue workload {workload} to child qdisc with handle {handle}.", LogWriter.Blocking);

        lock (_syncRoot)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].Handle.Equals(handle))
                {
                    _dataMap[i] = true;
                    _children[i].Enqueue(workload);
                    DebugLog.WriteDiagnostic($"Enqueued workload {workload} to child qdisc {_children[i]}.", LogWriter.Blocking);
                    return true;
                }
                if (_children[i].TryEnqueueByHandle(handle, workload))
                {
                    _dataMap[i] = true;
                    DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {_children[i]}.", LogWriter.Blocking);
                    return true;
                }
            }
        }
        DebugLog.WriteDiagnostic($"Could not enqueue workload {workload} to any child qdisc. No child qdisc with handle {handle} found.", LogWriter.Blocking);
        return false;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        DebugLog.WriteDiagnostic($"Trying to enqueue workload {workload} to round robin qdisc {this}.", LogWriter.Blocking);

        lock (_syncRoot)
        {
            for (int i = 0; i < _children.Length; i++)
            {
                if (_children[i].TryEnqueue(state, workload))
                {
                    _dataMap[i] = true;
                    DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {_children[i]}.", LogWriter.Blocking);
                    return true;
                }
            }
            return TryEnqueueDirect(state, workload);
        }
    }

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        lock (_syncRoot)
        {
            for (int i = 1; i < _children.Length; i++)
            {
                if (_children[i].Handle.Equals(handle))
                {
                    path.Add(new RoutingPathNode<THandle>(this, handle, i));
                    path.Complete(_children[i]);
                    return true;
                }
                if (_children[i].TryFindRoute(handle, ref path))
                {
                    path.Add(new RoutingPathNode<THandle>(this, handle, i));
                    return true;
                }
            }
            return false;
        }
    }

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (Predicate.Invoke(state))
        {
            EnqueueDirect(workload);
            return true;
        }
        return false;
    }

    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        _localQueue.Enqueue(workload);
        _dataMap[0] = true;
        DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to local queue ({_localQueue}).", LogWriter.Blocking);
    }

    public override bool TryAddChild(IClassifyingQdisc<THandle> child) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override bool RemoveChild(IClassifyingQdisc<THandle> child) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override bool TryRemoveChild(IClassifyingQdisc<THandle> child) => throw new NotSupportedException();

    /// <inheritdoc/>
    protected override bool ContainsChild(THandle handle) =>
        TryFindChild(handle, out _);

    /// <inheritdoc/>
    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child)
    {
        for (int i = 1; i < _children.Length; i++)
        {
            if (_children[i].Handle.Equals(handle))
            {
                child = _children[i];
                return true;
            }
            if (_children[i] is IClassfulQdisc<THandle> classfulChild && classfulChild.TryFindChild(handle, out child))
            {
                return true;
            }
        }
        child = null;
        return false;
    }

    protected override void OnWorkerTerminated(int workerId)
    {
        // reset the last child qdisc for this worker
        Volatile.Write(ref _localLasts[workerId], null);

        // forward to children, no lock needed. if children are removed then they don't need to be notified
        // and if new children are added, they shouldn't know about the worker anyway
        IClassifyingQdisc<THandle>[] children = _children;
        for (int i = 0; i < children.Length; i++)
        {
            children[i].OnWorkerTerminated(workerId);
        }

        base.OnWorkerTerminated(workerId);
    }

    protected override void DisposeManaged()
    {
        foreach (IClassifyingQdisc<THandle> child in _children)
        {
            child.Complete();
            child.Dispose();
        }
        _localLasts.AsSpan().Clear();
        base.DisposeManaged();
    }

    protected override void ChildrenToTreeString(StringBuilder builder, int indent)
    {
        builder.AppendIndent(indent).Append($"Local 0: ");
        ChildToTreeString(_localQueue, builder, indent);
        for (int i = 1; i < _children.Length; i++)
        {
            builder.AppendIndent(indent).Append($"Child {i}: ");
            ChildToTreeString(_children[i], builder, indent);
        }
    }
}