using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wkg.Collections.Concurrent;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Extensions;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Routing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class RoundRobinBitmapQdisc<THandle> : ClassfulQdisc<THandle>, IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    private readonly ThreadLocal<int?> __LAST_ENQUEUED_CHILD_INDEX = new();

    private readonly IQdisc?[] _localLasts;
    private readonly IClassifyingQdisc<THandle> _localQueue;

    private volatile IClassifyingQdisc<THandle>[] _children;
    private readonly ReaderWriterLockSlim _childrenLock;
    private readonly ConcurrentBitmap _dataMap;
    private int _rrIndex;
    private int _maxRoutingPathDepthEncountered = 4;

    public RoundRobinBitmapQdisc(THandle handle, Predicate<object?>? predicate, IClasslessQdiscBuilder localQueueBuilder, int maxConcurrency) : base(handle, predicate)
    {
        _localQueue = localQueueBuilder.BuildUnsafe(default(THandle), MatchNothingPredicate);
        _localLasts = new IQdisc[maxConcurrency];
        _children = [_localQueue];
        _childrenLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        _dataMap = new ConcurrentBitmap(1);
    }

    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    public override bool IsEmpty
    {
        get
        {
            using ILockOwnership readLock = _childrenLock.AcquireReadLock();
            return IsEmptyInternal;
        }
    }

    private bool IsEmptyInternal => _dataMap.IsEmptyUnsafe;

    public override int BestEffortCount
    {
        get
        {
            // we must ensure that no other thread is removing children
            // dequeue operations are not a problem, since we only need to provide a weakly consistent count!
            using ILockOwnership readLock = _childrenLock.AcquireReadLock();
            // we can take a shortcut here, if we established that all children are empty
            if (IsEmptyInternal)
            {
                return 0;
            }
            // get a local snapshot of the children array reference, other threads may still add new children which we don't care about here
            IClassifyingQdisc<THandle>[] children = _children;
            int count = 0;
            for (int i = 0; i < children.Length; i++)
            {
                count += children[i].BestEffortCount;
            }
            return count;
        }
    }

    // not supported. this is a classful qdisc that never contains workloads directly.
    // workloads are always contained in leaf qdiscs. classful qdiscs always have at least one child qdisc by default.
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();
        if (IsEmptyInternal)
        {
            // we know that all children are empty, so we can return false immediately
            DebugLog.WriteDiagnostic($"{this} qdisc is known to be empty, taking shortcut and returning false.", LogWriter.Blocking);
            workload = null;
            return false;
        }
        // if we have to backtrack, we can do so by dequeuing from the last child qdisc
        // that was dequeued from. If the last child qdisc is empty, we can't backtrack and continue
        // with the next child qdisc.
        if (backTrack && _localLasts[workerId]?.TryDequeueInternal(workerId, backTrack, out workload) is true)
        {
            DebugLog.WriteDiagnostic($"{this} Backtracking to last child qdisc {_localLasts[workerId]!.GetType().Name} ({_localLasts[workerId]}).", LogWriter.Blocking);
            return true;
        }
        // backtracking failed, or was not requested. We need to iterate over all child qdiscs.
        IClassifyingQdisc<THandle>[] children = _children;
        while (!IsEmptyInternal)
        {
            int index = Atomic.IncrementModulo(ref _rrIndex, children.Length);
            // if the qdisc is empty, we can skip it
            GuardedBitInfo bitInfo = _dataMap.GetBitInfoUnsafe(index);
            if (!bitInfo.IsSet)
            {
                // skip empty children
                continue;
            }
            IQdisc qdisc = children[index];
            // usually we should succeed first try, so we use the existing token from our read earlier
            byte token = bitInfo.Token;
            int i = 0;
            do
            {
                if (i != 0)
                {
                    // on subsequent tries, we need to refresh the token
                    DebugLog.WriteDebug($"{this}: emptiness bit map changed while attempting to declare child {qdisc} as empty. Attempts so far {i}. Resampling...", LogWriter.Blocking);
                    token = _dataMap.GetTokenUnsafe(index);
                }
                // get our assigned child qdisc
                if (qdisc.TryDequeueInternal(workerId, backTrack, out workload))
                {
                    DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {qdisc}.", LogWriter.Blocking);
                    // we found a workload, update the last child qdisc and reset the empty counter
                    _localLasts[workerId] = qdisc;
                    return true;
                }
                // the child seems to be empty, but we can't be sure.
                // attempt to update the emptiness bit map to reflect the new state
                DebugLog.WriteDiagnostic($"{this}: child {qdisc} seems to be empty. Updating emptiness bit map.", LogWriter.Blocking);
                i++;
            } while (!_dataMap.TryUpdateBitUnsafe(index, token, isSet: false));
            DebugLog.WriteDebug($"{this}: Emptiness state of child qdisc {qdisc} changed to empty.", LogWriter.Blocking);
        }
        // all children are empty
        DebugLog.WriteDebug($"{this}: All children are empty.", LogWriter.Blocking);
        workload = null;
        return false;
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();
        if (IsEmptyInternal)
        {
            // we know that all children are empty, so we can return false immediately
            DebugLog.WriteDiagnostic($"{this} qdisc is known to be empty, taking shortcut and returning false.", LogWriter.Blocking);
            workload = null;
            return false;
        }
        while (!IsEmptyInternal)
        {
            IClassifyingQdisc<THandle>[] children = _children;

            // this one is easier than TryDequeueInternal, since we operate entirely read-only and we have out own local state
            // in theory, we could participate in the empty counter tracking, but that's not necessary
            int index = Volatile.Read(ref _rrIndex);
            int i;
            for (i = 0; i < children.Length; i++, index = (index + 1) % children.Length)
            {
                // we can use the unsafe version here, since we are holding a read lock (the bitmap structure won't change)
                if (_dataMap.IsBitSetUnsafe(i) && children[index].TryPeekUnsafe(workerId, out workload))
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

        using ILockOwnership readLock = _childrenLock.AcquireReadLock();

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
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();
        IClassifyingQdisc<THandle>[] children = _children;
        RoutingPath<THandle> path = new(Volatile.Read(ref _maxRoutingPathDepthEncountered));
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
            if (child.TryFindRoute(handle, ref path))
            {
                // ensure that the path is complete and valid
                WorkloadSchedulingException.ThrowIfRoutingPathLeafIsInvalid(path.Leaf, handle);

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
        using (ILockOwnership readLock = _childrenLock.AcquireReadLock())
        {
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
                        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {child} reported to be able to classify workload {workload}, but failed to do so.");
                        Debug.Fail(exception.Message);
                        DebugLog.WriteException(exception, LogWriter.Blocking);
                        // we are on the enqueueing thread, so we can just throw here
                        throw exception;
                    }
                    DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {child}.", LogWriter.Blocking);
                    return true;
                }
            }
        }
        return TryEnqueueDirect(state, workload);
    }

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();

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
        // the local queue is a qdisc itself, so we can enqueue directly to it
        // it will call back to us with OnWorkScheduled, so we can reset the empty counter there
        // we will never need a lock here, since the local queue itself is thread-safe and cannot
        // be removed from the children array.
        const int localQueueIndex = 0;
        __LAST_ENQUEUED_CHILD_INDEX.Value = localQueueIndex;
        _localQueue.Enqueue(workload);
        DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to local queue ({_localQueue}).", LogWriter.Blocking);
    }

    /// <inheritdoc/>
    protected override void OnWorkScheduled()
    {
        if (_childrenLock.IsWriteLockHeld)
        {
            // we are inside a callback from some child modification method
            // we don't need to do anything here, as the child modification method will take care of the emptiness tracking
            // we must also break the notification chain here, as no "real" enqueueing operation happened
            return;
        }
        // we are inside a callback of an enqueuing thread
        // load the index of the child that was just enqueued to
        int? lastEnqueuedChildIndex = __LAST_ENQUEUED_CHILD_INDEX.Value;
        if (lastEnqueuedChildIndex is null)
        {
            // this should never happen, as this method can only be part of the enqueueing call stack
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: {nameof(__LAST_ENQUEUED_CHILD_INDEX)} is null.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            // we can actually just throw here, since we aren't in a worker thread
            throw new NotSupportedException("This scheduler does not support scheduling workloads directly onto child qdiscs. Please use the methods provided by the parent workload factory.", exception);
        }
        // clear the empty flag for the child that was just enqueued to
        int index = lastEnqueuedChildIndex.Value;
        // no token is required, as we just force the bit to be set, we can use the unsafe version here since a parent of our call stack must be holding a read lock
        // worker threads attempting to mark this child as empty will just fail to do so as their token will be invalidated by us
        // so no ABA problem here (not empty -> worker finds no workload -> we set it to not empty -> worker tries to set it to empty -> worker fails)
        _dataMap.UpdateBitUnsafe(index, isSet: true);
        // reset the last enqueued child index
        __LAST_ENQUEUED_CHILD_INDEX.Value = null;
        DebugLog.WriteDebug($"{this}: cleared empty flag for {(index == 0 ? this : _children[index])}.", LogWriter.Blocking);
        base.OnWorkScheduled();
    }

    public override bool TryAddChild(IClassifyingQdisc<THandle> child)
    {
        using ILockOwnership writeLock = _childrenLock.AcquireWriteLock();
        DebugLog.WriteDiagnostic($"Trying to add child qdisc {child} to round robin qdisc {this}.", LogWriter.Blocking);

        if (TryFindChildUnsafe(child.Handle, out _))
        {
            DebugLog.WriteWarning($"{this}: failed to add child {child} because it is already a child of this qdisc.", LogWriter.Blocking);
            return false;
        }

        // link the child qdisc to the parent qdisc first
        child.InternalInitialize(this);

        // no lock needed, a new array is created and the reference is CASed in
        // contention is unlikely, since this method is only called when a new child qdisc is created
        // and added to the parent qdisc, which is not a frequent operation
        IClassifyingQdisc<THandle>[] children = _children;
        // child not present, add it
        _children = [.. children, child];
        // update the emptiness tracking
        // instead of inserting a new bit, we just grow and update the last bit
        // this is a cheaper operation as we don't need to hold a write lock for the update
        // and growing the bit map by one bit is a cheap operation if we don't hit any segment or cluster boundaries
        Debug.Assert(_children.Length == children.Length + 1);
        _dataMap.Grow(additionalSize: 1);
        _dataMap.UpdateBitUnsafe(_children.Length - 1, isSet: false);
        return true;
    }

    /// <inheritdoc/>
    public override bool RemoveChild(IClassifyingQdisc<THandle> child) =>
        // block up to 60 seconds to allow the child to become empty
        RemoveChildCore(child, Timeout.Infinite);

    /// <inheritdoc/>
    public override bool TryRemoveChild(IClassifyingQdisc<THandle> child) =>
        RemoveChildCore(child, 0);

    private bool RemoveChildCore(IClassifyingQdisc<THandle> child, int millisecondsTimeout)
    {
        // before locking on the write lock, check if the child is even present. if it's not, we can return early
        if (!ContainsChild(child.Handle))
        {
            return false;
        }
        int startTime = Environment.TickCount;
        // wait for child to be empty
        if (!Wait.Until(() => child.IsEmpty, millisecondsTimeout))
        {
            return false;
        }
        // child is empty, attempt to remove it
        using ILockOwnership writeLock = _childrenLock.AcquireWriteLock();

        // check if child is still there
        if (!TryFindChildUnsafe(child.Handle, out _))
        {
            return false;
        }

        // mark the child as completed (all new scheduling attempts will fail)
        child.Complete();
        // someone may have scheduled new workloads in the meantime
        // we preserve them by moving them to the local queue
        // this may break the intended scheduling order, but it is better than losing workloads
        // also that is acceptable, as it should happen very rarely and only if the user is doing something wrong
        // we simply impersonate worker 0 here, as we have exclusive access to the child qdisc anyway
        bool childHasWorkloads = false;
        while (child.TryDequeueInternal(0, false, out AbstractWorkloadBase? workload))
        {
            // enqueue the workloads in the local queue
            _localQueue.Enqueue(workload);
            childHasWorkloads = true;
        }
        if (childHasWorkloads)
        {
            // we just moved workloads from the child to the local queue
            const int localQueueIndex = 0;
            _dataMap.UpdateBitUnsafe(localQueueIndex, isSet: true);
        }

        IClassifyingQdisc<THandle>[] oldChildren = _children;
        IClassifyingQdisc<THandle>[] newChildren = new IClassifyingQdisc<THandle>[oldChildren.Length - 1];
        int index = -1;
        for (int i = 0; i < oldChildren.Length && i < newChildren.Length; i++)
        {
            if (!oldChildren[i].Handle.Equals(child.Handle))
            {
                newChildren[i] = oldChildren[i];
            }
            else
            {
                index = i;
            }
        }
        Debug.Assert(index >= 0);

        // update the emptiness tracking
        _dataMap.RemoveBitAt(index, shrink: true);
        _children = newChildren;
        return true;
    }

    /// <inheritdoc/>
    protected override bool ContainsChild(THandle handle) =>
        TryFindChild(handle, out _);

    /// <inheritdoc/>
    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child)
    {
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();
        return TryFindChildUnsafe(handle, out child);
    }

    private bool TryFindChildUnsafe(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child)
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
        _childrenLock.Dispose();
        IClassifyingQdisc<THandle>[] children = Interlocked.Exchange(ref _children, []);
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
        using ILockOwnership readLock = _childrenLock.AcquireReadLock();
        builder.AppendIndent(indent).Append($"Local 0: ");
        ChildToTreeString(_localQueue, builder, indent);
        for (int i = 1; i < _children.Length; i++)
        {
            builder.AppendIndent(indent).Append($"Child {i}: ");
            ChildToTreeString(_children[i], builder, indent);
        }
    }
}