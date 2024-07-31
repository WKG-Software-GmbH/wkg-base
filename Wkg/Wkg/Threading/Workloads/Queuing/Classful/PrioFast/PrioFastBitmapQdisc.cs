using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wkg.Collections.Concurrent;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements a simple priority scheduling algorithm to dequeue workloads from its children.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class PrioFastBitmapQdisc<THandle> : ClassfulQdisc<THandle>, IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "thread-local variable")]
    private readonly ThreadLocal<int?> __LAST_ENQUEUED_CHILD_INDEX = new();

    private readonly ConcurrentBitmap _dataMap;
    private readonly IQdisc?[] _localLasts;
    private readonly IClassifyingQdisc<THandle> _localQueue;
    private readonly IClassifyingQdisc<THandle>[] _children;

    private int _maxRoutingPathDepthEncountered = 4;

    public PrioFastBitmapQdisc(THandle handle, Predicate<object?>? predicate, IClasslessQdiscBuilder localQueueBuilder, IClassifyingQdisc<THandle>[] children, int maxConcurrency) : base(handle, predicate)
    {
        _localQueue = localQueueBuilder.BuildUnsafe(default(THandle), MatchNothingPredicate);
        _localLasts = new IQdisc[maxConcurrency];
        _children = [_localQueue, .. children];
        foreach (IClassifyingQdisc<THandle> child in children)
        {
            BindChildQdisc(child);
        }
        _dataMap = new ConcurrentBitmap(_children.Length);
    }

    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    public override bool IsEmpty => IsEmptyInternal;

    private bool IsEmptyInternal => _dataMap.IsEmptyUnsafe;

    public override int BestEffortCount
    {
        get
        {
            // we can take a shortcut here, if we established that all children are empty
            if (IsEmptyInternal)
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

    // not supported.
    // would only need to consider the local queue, since this
    // method is only called on the direct parent of a workload.
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // if we have to backtrack, we can do so by dequeuing from the last child qdisc
        // that was dequeued from. If the last child qdisc is empty, we can't backtrack and continue
        // with the next child qdisc.
        if (backTrack && !IsEmptyInternal && _localLasts[workerId]?.TryDequeueInternal(workerId, backTrack, out workload) is true)
        {
            DebugLog.WriteDiagnostic($"{this} Backtracking to last child qdisc {_localLasts[workerId]!.GetType().Name} ({_localLasts[workerId]}).", LogWriter.Blocking);
            return true;
        }
        // backtracking failed, or was not requested. We need to iterate over all child qdiscs.
        IClassifyingQdisc<THandle>[] children = _children;
        while (!IsEmptyInternal)
        {
            for (int index = 0; index < children.Length; index++)
            {
                // if the qdisc is empty, we can skip it
                GuardedBitInfo bitInfo = _dataMap.GetBitInfoUnsafe(index);
                if (!bitInfo.IsSet)
                {
                    // skip empty children
                    continue;
                }
                // usually we should succeed first try, so we use the existing token from our read earlier
                byte token = bitInfo.Token;
                IClassifyingQdisc<THandle> child = children[index];
                int expiredTokens = 0;
                do
                {
                    if (expiredTokens != 0)
                    {
                        // on subsequent tries, we need to refresh the token
                        DebugLog.WriteDebug($"{this}: emptiness bit map changed while attempting to declare child {child} as empty. Attempts so far {expiredTokens}. Resampling...", LogWriter.Blocking);
                        token = _dataMap.GetTokenUnsafe(index);
                    }
                    // get our assigned child qdisc
                    if (child.TryDequeueInternal(workerId, backTrack, out workload))
                    {
                        DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {child}.", LogWriter.Blocking);
                        // we found a workload, update the last child qdisc and reset the empty counter
                        _localLasts[workerId] = child;
                        return true;
                    }
                    // the child seems to be empty, but we can't be sure.
                    // attempt to update the emptiness bit map to reflect the new state
                    DebugLog.WriteDiagnostic($"{this}: child {child} seems to be empty. Updating emptiness bit map.", LogWriter.Blocking);
                    expiredTokens++;
                } while (!_dataMap.TryUpdateBitUnsafe(index, token, isSet: false));
                DebugLog.WriteDebug($"{this}: Emptiness state of child qdisc {child} changed to empty.", LogWriter.Blocking);
            }
        }
        // all children are empty
        DebugLog.WriteDebug($"{this}: All children are empty.", LogWriter.Blocking);
        workload = null;
        return false;
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        while (!IsEmptyInternal)
        {
            IClassifyingQdisc<THandle>[] children = _children;

            // this one is easier than TryDequeueInternal, since we operate entirely read-only and we have out own local state
            // in theory, we could participate in the empty counter tracking, but that's not necessary
            for (int i = 0; i < children.Length; i++)
            {
                // we can use the unsafe version here, since we are holding a read lock (the bitmap structure won't change)
                if (_dataMap.IsBitSetUnsafe(i) && children[i].TryPeekUnsafe(workerId, out workload))
                {
                    return true;
                }
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

        IClassifyingQdisc<THandle>[] children = _children;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].CanClassify(state))
            {
                return true;
            }
        }
        return false;
    }

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload)
    {
        DebugLog.WriteDiagnostic($"{this} Trying to enqueue workload {workload} to child qdisc with handle {handle}.", LogWriter.Blocking);

        // only lock the children array while we need to access it
        IClassifyingQdisc<THandle>[] children = _children;
        RoutingPath<THandle> path = new(Volatile.Read(in _maxRoutingPathDepthEncountered));
        // start at 1, since the local queue is always the first child and the local queue has "our" handle
        // so checking us/our local queue is redundant
        for (int i = 1; i < children.Length; i++)
        {
            IClassifyingQdisc<THandle> child = children[i];
            if (child.Handle.Equals(handle))
            {
                // set up the index of the child that we will enqueue to to allow emptiness tracking to update the correct bit
                __LAST_ENQUEUED_CHILD_INDEX.Value = i;
                child.Enqueue(workload);
                DebugLog.WriteDiagnostic($"Enqueued workload {workload} to child qdisc {child}.", LogWriter.Blocking);
                goto SUCCESS;
            }
            // we must first check if the child can enqueue the workload.
            // then we must prepare everything for the enqueueing operation.
            // only then can we actually enqueue the workload.
            // in order to achieve this we construct a routing path and then directly enqueue the workload to the child.
            // using a routing path allows us to avoid having to do the same work twice.
            if (child.TryFindRoute(handle, ref path) && path.Leaf is not null)
            {
                // update the emptiness tracking
                // the actual reset happens in the OnWorkScheduled callback, but we need to
                // set up the index of the child that was just enqueued to for that
                __LAST_ENQUEUED_CHILD_INDEX.Value = i;

                // we need to call WillEnqueueFromRoutingPath on all nodes in the path
                // failure to do so may result in incorrect emptiness tracking of the child qdiscs
                foreach (ref readonly RoutingPathNode<THandle> node in path)
                {
                    node.Qdisc.WillEnqueueFromRoutingPath(in node, workload);
                }
                // enqueue the workload to the leaf
                path.Leaf.Enqueue(workload);
                Atomic.WriteMaxFast(ref _maxRoutingPathDepthEncountered, path.Count);
                DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {child}.", LogWriter.Blocking);
                goto SUCCESS;
            }
        }
        DebugLog.WriteDiagnostic($"Could not enqueue workload {workload} to any child qdisc. No child qdisc with handle {handle} found.", LogWriter.Blocking);
        path.Dispose();
        return false;
    SUCCESS:
        path.Dispose();
        return true;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        DebugLog.WriteDiagnostic($"Trying to enqueue workload {workload} to round robin qdisc {this}.", LogWriter.Blocking);

        // only lock the children array while we need to access it
        IClassifyingQdisc<THandle>[] children = _children;
        for (int i = 0; i < children.Length; i++)
        {
            IClassifyingQdisc<THandle> child = children[i];
            if (child.CanClassify(state))
            {
                // update the emptiness tracking
                // the actual reset happens in the OnWorkScheduled callback, but we need to
                // set up the index of the child that was just enqueued to for that
                __LAST_ENQUEUED_CHILD_INDEX.Value = i;
                if (!child.TryEnqueue(state, workload))
                {
                    // this should never happen, as we already checked if the child can classify the workload
                    ThrowClassificationFailure(child, workload);
                }
                DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {child}.", LogWriter.Blocking);
                return true;
            }
        }
        return TryEnqueueDirect(state, workload);
    }

    [DoesNotReturn]
    private static void ThrowClassificationFailure(IClassifyingQdisc<THandle> child, AbstractWorkloadBase workload)
    {
        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {child} reported to be able to classify workload {workload}, but failed to do so.");
        Debug.Fail(exception.Message);
        DebugLog.WriteException(exception, LogWriter.Blocking);
        // we are on the enqueueing thread, so we can just throw here
        throw exception;
    }

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        IClassifyingQdisc<THandle>[] children = _children;
        for (int i = 1; i < children.Length; i++)
        {
            IClassifyingQdisc<THandle> child = children[i];
            if (child.Handle.Equals(handle))
            {
                path.Add(new RoutingPathNode<THandle>(this, handle, i));
                path.Complete(child);
                return true;
            }
            if (child.TryFindRoute(handle, ref path))
            {
                path.Add(new RoutingPathNode<THandle>(this, handle, i));
                return true;
            }
        }
        return false;
    }

    protected override void WillEnqueueFromRoutingPath(ref readonly RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload)
    {
        int index = routingPathNode.Offset;
        __LAST_ENQUEUED_CHILD_INDEX.Value = index;
        DebugLog.WriteDiagnostic($"{this}: expecting to enqueue workload {workload} to child {_children[index]} via routing path.", LogWriter.Blocking);
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
        const int LOCAL_QUEUE_INDEX = 0;
        __LAST_ENQUEUED_CHILD_INDEX.Value = LOCAL_QUEUE_INDEX;
        _localQueue.Enqueue(workload);
        DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to local queue ({_localQueue}).", LogWriter.Blocking);
    }

    /// <inheritdoc/>
    protected override void OnWorkScheduled()
    {
        // we are inside a callback of an enqueuing thread
        // load the index of the child that was just enqueued to
        int? lastEnqueuedChildIndex = __LAST_ENQUEUED_CHILD_INDEX.Value;
        if (lastEnqueuedChildIndex is null)
        {
            // this should never happen, as this method can only be part of the enqueueing call stack
            ThrowLastEnqueuedChildIndexNull();
        }
        // clear the empty flag for the child that was just enqueued to
        int index = lastEnqueuedChildIndex.Value;
        // worker threads attempting to mark this child as empty will just fail to do so as their token will be invalidated by us
        // so no ABA problem here (not empty -> worker finds no workload -> we set it to not empty -> worker tries to set it to empty -> worker fails)
        _dataMap.UpdateBitUnsafe(index, isSet: true);
        // reset the last enqueued child index
        __LAST_ENQUEUED_CHILD_INDEX.Value = null;
        DebugLog.WriteDebug($"{this}: cleared empty flag for {(index == 0 ? this : _children[index])}.", LogWriter.Blocking);
        base.OnWorkScheduled();
    }

    [DoesNotReturn]
    private static void ThrowLastEnqueuedChildIndexNull()
    {
        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: {nameof(__LAST_ENQUEUED_CHILD_INDEX)} is null.");
        DebugLog.WriteException(exception, LogWriter.Blocking);
        // we can actually just throw here, since we aren't in a worker thread
        throw new NotSupportedException("This scheduler does not support scheduling workloads directly onto child qdiscs. Please use the methods provided by the parent workload factory.", exception);
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
        IClassifyingQdisc<THandle>[] children = _children;
        for (int i = 1; i < children.Length; i++)
        {
            child = children[i];
            if (child.Handle.Equals(handle))
            {
                return true;
            }
            if (child is IClassfulQdisc<THandle> classfulChild && classfulChild.TryFindChild(handle, out child))
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
        IClassifyingQdisc<THandle>[] children = _children;
        foreach (IClassifyingQdisc<THandle> child in children)
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