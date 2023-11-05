using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Queuing.Classful.Internals;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class RoundRobinQdisc<THandle> : ClassfulQdisc<THandle>, IClassfulQdisc<THandle>
    where THandle : unmanaged
{
    private readonly IQdisc?[] _localLasts;
    private readonly IClasslessQdisc<THandle> _localQueue;
    private readonly Predicate<object?> _predicate;

    private IChildClassification<THandle>[] _children;
    private readonly ReaderWriterLockSlim _childrenLock;
    private readonly EmptyCounter _emptyCounter;
    private int _rrIndex;
    private int _criticalDequeueSection;

    public RoundRobinQdisc(THandle handle, Predicate<object?> predicate, IClasslessQdiscBuilder localQueueBuilder, int maxConcurrency) : base(handle)
    {
        _localQueue = localQueueBuilder.BuildUnsafe(default(THandle));
        _localLasts = new IQdisc[maxConcurrency];
        _children = new IChildClassification<THandle>[] { new NoChildClassification<THandle>(_localQueue) };
        _predicate = predicate;
        _childrenLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        _emptyCounter = new EmptyCounter();
    }

    /// <inheritdoc/>
    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    /// <inheritdoc/>
    public override bool IsEmpty => Count == 0;

    /// <inheritdoc/>
    public override int Count
    {
        get
        {
            // we can take a shortcut here, if we established that all children are empty
            if (IsKnownEmptyVolatile)
            {
                return 0;
            }
            try
            {
                // we must ensure that no other thread is removing children
                // dequeue operations are not a problem, since we only need to provide a weakly consistent count!
                _childrenLock.EnterUpgradeableReadLock();
                int count = CountChildrenUnsafe();
                if (count == 0)
                {
                    // we need to actually block any enqueue operations while we're counting
                    // otherwise we may miss workloads that are enqueued while we're counting
                    // upgrade to a write lock
                    try
                    {
                        _childrenLock.EnterWriteLock();
                        count = CountChildrenUnsafe();
                    }
                    finally
                    {
                        _childrenLock.ExitWriteLock();
                    }
                }
                return count;
            }
            finally
            {
                _childrenLock.ExitUpgradeableReadLock();
            }
        }
    }

    /// <summary>
    /// Only call this method if you have a read or write lock on <see cref="_childrenLock"/>.
    /// </summary>
    private int CountChildrenUnsafe()
    {
        // get a local snapshot of the children array, other threads may still add new children which we don't care about here
        IChildClassification<THandle>[] children = Volatile.Read(ref _children);
        int count = 0;
        for (int i = 0; i < children.Length; i++)
        {
            count += children[i].Qdisc.Count;
        }
        return count;
    }

    private bool IsKnownEmptyVolatile
    {
        get
        {
            IChildClassification<THandle>[] children1, children2;
            uint emptyCounter;
            while (true)
            {
                children1 = Volatile.Read(ref _children);
                emptyCounter = _emptyCounter.GetCount();
                children2 = Volatile.Read(ref _children);
                if (ReferenceEquals(children1, children2))
                {
                    // the children array didn't change while we were reading it
                    // we can use the empty counter to determine if the qdisc is empty
                    return emptyCounter >= children1.Length;
                }
            }
        }
    }

    /// <inheritdoc/>
    // not supported.
    // would only need to consider the local queue, since this
    // method is only called on the direct parent of a workload.
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    /// <inheritdoc/>
    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        if (IsKnownEmptyVolatile)
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
        while (true)
        {
            // we operate lock-free on a local snapshot of the children array
            // by contract, elements in the children array are immutable, so we don't need to worry about them changing
            // however, the children array itself may change via a CAS operation, so we must only operate on the local snapshot
            // of the reference. If the reference changes, we need to start over, since we don't know which child qdiscs we already
            // iterated over.
            // however, reference changes are rare, so we can afford the risk of having to start over.
            IChildClassification<THandle>[] children = Volatile.Read(ref _children);

            // loop until we find a workload or until we meet requirements for worker termination
            // if the children array changes while we're looping, we need to start over
            do
            {
                // this is our local round robin index. it is not guaranteed to be in range, since we are reading a *snapshot*
                // the modulo is only applied after the increment, so we need to check if the index is in range
                // NOTE: that the _rrIndex is *post-incremented*, so our index is _rrIndex - 1
                // (well it would be, if it wasn't for the modulo)
                int index = Atomic.IncrementModulo(ref _rrIndex, children.Length);
                if (index >= children.Length)
                {
                    // we're out of range, the old round robin index was stale
                    // retry with the updated round robin index which is guaranteed to be in range
                    // (unless the array changed again, but that's unlikely)
                    DebugLog.WriteDiagnostic($"Round robin index out of range, retrying with updated index.", LogWriter.Blocking);
                    // we need to break here to ensure that we update our local snapshot of the children array as well!
                    break;
                }
                // the empty counter may be reset by another thread between the time we read it and the time we increment it.
                // for this reason, we need to get a token equivalent to the current "counter generation". we then use this token
                // as a ticket to increment the counter. if the counter generation changed while we were incrementing the counter,
                // the increment will fail and we need to start over.
                uint token = _emptyCounter.GetToken();
                // we need to enter a critical section to keep track of how many threads are currently attempting to dequeue a workload.
                // if we assume that the qdisc is empty, we need to wait until all threads that entered the critical section before us
                // have left it. otherwise, slower but valid dequeue operations may be missed.
                CriticalSection criticalSection = CriticalSection.Enter(ref _criticalDequeueSection);
                // get our assigned child qdisc
                IQdisc qdisc = children[index].Qdisc;
                if (qdisc.TryDequeueInternal(workerId, backTrack, out workload))
                {
                    DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {qdisc}.", LogWriter.Blocking);
                    // we found a workload, update the last child qdisc and reset the empty counter
                    _localLasts[workerId] = children[index].Qdisc;
                    // reset the empty counter and start a new counter generation *before* we leave the critical dequeue section
                    _emptyCounter.Reset();
                    // leave the critical dequeue section
                    criticalSection.Exit();
                    return true;
                }
                // leave the critical dequeue section
                criticalSection.Exit();
                // we didn't find a workload, increment the empty counter
                // this increment may fail if the counter generation changed while we were dequeuing from the child qdisc
                // however, if the incremented value exceeds the number of children, we can assume that the qdisc is empty
                // (after we establish consensus with the other threads, that is)
                if (_emptyCounter.TryIncrement(token) >= children.Length)
                {
                    // we meet requirements for worker termination
                    // *however* it's possible that one worker just took forever to dequeue a workload and that the 
                    // qdisc is not actually empty. We can't just return false here, since that would cause the worker
                    // to terminate permanently (until a new workload is scheduled). So we need to spin until we're sure
                    // that the qdisc is actually empty (wait for the critical dequeue section to be empty)
                    // we need to do this to establish consensus with the other threads before we declare the qdiscs as empty.
                    // once they are declared empty, we will never check again until new workloads are scheduled. So this is
                    // kind of a big deal.
                    // we don't technically need to wait until it's completely empty, we just need to wait for any threads that
                    // entered the critical dequeue section while we were in the critical dequeue section to leave it.
                    // so there is potential for optimization here.
                    DebugLog.WriteDiagnostic($"{this} All child qdiscs are empty, waiting for critical dequeue section to be empty.", LogWriter.Blocking);
                    criticalSection.SpinUntilEmpty();
                    // critical section is empty. RESAMPLE! (the other threads may have dequeued workloads in the meantime)
                    DebugLog.WriteDiagnostic($"{this} Critical dequeue section is empty, resampling children.", LogWriter.Blocking);
                    if (IsKnownEmptyVolatile)
                    {
                        // well, we trid our best, but it seems like the qdisc is actually empty
                        // give up for now we must assume that the other threads did their checks
                        // correctly and that the qdisc is indeed empty.
                        // this qdisc is now considered empty until new workloads are scheduled.
                        workload = null;
                        _localLasts[workerId] = null;
                        DebugLog.WriteDiagnostic($"{this} Qdisc is empty, returning false.", LogWriter.Blocking);
                        return false;
                    }

                    // the qdisc is not empty! continue with the next iteration
                    DebugLog.WriteDebug($"{this} Qdisc is not empty, continuing with next iteration.", LogWriter.Blocking);
                }
                // the qdisc is not empty, but we didn't find a workload. continue with the next iteration
            } while (ReferenceEquals(children, Interlocked.CompareExchange(ref _children, children, children)));
            // the children array changed while we were iterating over it
            DebugLog.WriteDebug($"Children array changed while iterating over it, resampling children.", LogWriter.Blocking);
        }
    }

    /// <inheritdoc/>
    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        try
        {
            // prevent children from being removed while we're iterating over them
            // new children can still be added, but that's not a problem
            _childrenLock.EnterReadLock();
            DebugLog.WriteDiagnostic($"Trying to enqueue workload {workload} to round robin qdisc {this}.", LogWriter.Blocking);

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].TryEnqueue(state, workload))
                {
                    DebugLog.WriteDiagnostic($"Enqueued workload {workload} to child qdisc {children[i].Qdisc}.", LogWriter.Blocking);
                    return true;
                }
            }
            DebugLog.WriteDiagnostic($"Could not enqueue workload {workload} to any child qdisc.", LogWriter.Blocking);
        }
        finally
        {
            _childrenLock.ExitReadLock();
        }
        return false;
    }

    /// <inheritdoc/>
    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (_predicate(state))
        {
            DebugLog.WriteDiagnostic($"Enqueuing workload {workload} directly to round robin qdisc {this}.", LogWriter.Blocking);
            EnqueueDirect(workload);
            return true;
        }
        DebugLog.WriteDiagnostic($"Could not enqueue workload {workload} directly to round robin qdisc {this}.", LogWriter.Blocking);
        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void EnqueueDirect(AbstractWorkloadBase workload) =>
        // the local queue is a qdisc itself, so we can enqueue directly to it
        // it will call back to us with OnWorkScheduled, so we can reset the empty counter there
        // we will never need a lock here, since the local queue itself is thread-safe and cannot
        // be removed from the children array.
        _localQueue.Enqueue(workload);

    /// <inheritdoc/>
    public override bool TryAddChild(IClasslessQdisc<THandle> child)
    {
        IChildClassification<THandle> classifiedChild = new NoChildClassification<THandle>(child);
        return TryAddChildCore(classifiedChild);
    }

    /// <inheritdoc/>
    public override bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate)
    {
        IChildClassification<THandle> classifiedChild = new ChildClassification<THandle>(child, predicate);
        return TryAddChildCore(classifiedChild);
    }

    /// <inheritdoc/>
    public override bool TryAddChild(IClassfulQdisc<THandle> child)
    {
        IChildClassification<THandle> classifiedChild = new ClassfulChildClassification<THandle>(child);
        return TryAddChildCore(classifiedChild);
    }

    private bool TryAddChildCore(IChildClassification<THandle> child)
    {
        DebugLog.WriteDiagnostic($"Trying to add child qdisc {child.Qdisc} to round robin qdisc {this}.", LogWriter.Blocking);
        // link the child qdisc to the parent qdisc first
        child.Qdisc.InternalInitialize(this);

        // no lock needed, a new array is created and the reference is CASed in
        // contention is unlikely, since this method is only called when a new child qdisc is created
        // and added to the parent qdisc, which is not a frequent operation
        IChildClassification<THandle>[] children, newChildren;
        do
        {
            // get local readonly snapshot of the children array
            children = Volatile.Read(ref _children);
            // check if the child is already present
            // we need to repeat this check in case another thread added the same child in the meantime
            // (this shouldn't happen, but it's possible. people do weird things sometimes)
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Qdisc.Handle.Equals(child.Qdisc.Handle))
                {
                    // child already present
                    DebugLog.WriteDiagnostic($"Child qdisc {child.Qdisc} already present in round robin qdisc {this}.", LogWriter.Blocking);
                    return false;
                }
            }
            // child not present, add it
            newChildren = new IChildClassification<THandle>[children.Length + 1];
            Array.Copy(children, newChildren, children.Length);
            newChildren[^1] = child;
            // try to CAS the new array in, if it fails, try again
        } while (Interlocked.CompareExchange(ref _children, newChildren, children) != children);
        // CAS succeeded, we're done
        DebugLog.WriteDiagnostic($"Added child qdisc {child.Qdisc} to round robin qdisc {this}.", LogWriter.Blocking);
        return true;
    }

    /// <inheritdoc/>
    public override bool TryRemoveChild(IClasslessQdisc<THandle> child) =>
        RemoveChildInternal(child, -1);

    /// <inheritdoc/>
    public override bool RemoveChild(IClasslessQdisc<THandle> child) =>
        // block up to 60 seconds to allow the child to become empty
        RemoveChildInternal(child, 60 * 1000);

    private bool RemoveChildInternal(IClasslessQdisc<THandle> child, int millisecondsTimeout)
    {
        DebugLog.WriteDiagnostic($"Trying to remove child qdisc {child} from round robin qdisc {this}.", LogWriter.Blocking);
        // before locking, check if the child is even present. if it's not, we can return early
        if (!this.To<IClassfulQdisc<THandle>>().ContainsChild(child.Handle))
        {
            DebugLog.WriteDiagnostic($"Quickly returning false, child qdisc {child} not present in round robin qdisc {this}.", LogWriter.Blocking);
            return false;
        }
        try
        {
            // that's unfortunate, we need to acquire the write lock
            _childrenLock.EnterWriteLock();

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            // check if the child is present
            // we need to repeat this check in case another thread removed the same child in the meantime
            // (this shouldn't happen, but it's possible. people do weird things sometimes)
            int childIndex = -1;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].Qdisc.Handle.Equals(child.Handle))
                {
                    childIndex = i;
                    break;
                }
            }
            if (childIndex == -1)
            {
                // child not present
                DebugLog.WriteWarning($"Child qdisc {child} not present in round robin qdisc {this} but we already marked it as complete. This is a bug in the qdisc implementation.", LogWriter.Blocking);
                return false;
            }
            // child present, remove it
            if (!child.IsEmpty)
            {
                DebugLog.WriteDiagnostic($"Child qdisc {child} is not empty, waiting for it to become empty.", LogWriter.Blocking);
                if (millisecondsTimeout <= 0 || !SpinWait.SpinUntil(() => child.IsEmpty, millisecondsTimeout))
                {
                    // can't guarantee that the child is empty, so we can't remove it
                    // we waited for the child to become empty, but it didn't
                    DebugLog.WriteDiagnostic($"Returning false, child qdisc {child} is not empty and either no timeout was specified or the timeout expired.", LogWriter.Blocking);
                    return false;
                }
                // the child was empty after waiting a bit, so we can remove it
                // prevent new workloads from being scheduled to the child after we lift the lock
                child.Complete();
            }

            IChildClassification<THandle>[] newChildren = new IChildClassification<THandle>[children.Length - 1];
            children.AsSpan(0, childIndex).CopyTo(newChildren);
            children.AsSpan(childIndex + 1).CopyTo(newChildren.AsSpan(childIndex));

            _children = newChildren;
            DebugLog.WriteDiagnostic($"Removed child qdisc {child} from round robin qdisc {this}.", LogWriter.Blocking);
            return true;
        }
        finally
        {
            _childrenLock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    protected override bool ContainsChild(THandle handle) =>
        TryFindChild(handle, out _);

    /// <inheritdoc/>
    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child)
    {
        try
        {
            _childrenLock.EnterReadLock();

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            for (int i = 0; i < children.Length; i++)
            {
                child = children[i].Qdisc;
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
        finally
        {
            _childrenLock.ExitReadLock();
        }
    }

    protected override void OnWorkerTerminated(int workerId)
    {
        // reset the last child qdisc for this worker
        Volatile.Write(ref _localLasts[workerId], null);

        // forward to children, no lock needed. if children are removed then they don't need to be notified
        // and if new children are added, they shouldn't know about the worker anyway
        IChildClassification<THandle>[] children = Volatile.Read(ref _children);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].Qdisc.OnWorkerTerminated(workerId);
        }

        base.OnWorkerTerminated(workerId);
    }

    /// <inheritdoc/>
    protected override void OnWorkScheduled()
    {
        _emptyCounter.Reset();
        base.OnWorkScheduled();
    }

    private class EmptyCounter
    {
        // the emptiness counter is a 64 bit value that is split into two parts:
        // the first 32 bits are the generation counter, the last 32 bits are the actual counter
        // the generation counter is incremented whenever the counter is reset
        // we use a single 64 bit value to allow for atomic operations on both parts
        private ulong _state;

        public void Reset()
        {
            ulong state, newState;
            do
            {
                state = Volatile.Read(ref _state);
                uint generation = (uint)(state >> 32);
                newState = (ulong)(generation + 1) << 32;
            } while (Interlocked.CompareExchange(ref _state, newState, state) != state);
            DebugLog.WriteDiagnostic($"Reset emptiness counter. Current token: {newState >> 32}.", LogWriter.Blocking);
        }

        /// <summary>
        /// Increments the counter if the specified token is valid.
        /// </summary>
        /// <param name="token">The token previously obtained from <see cref="GetToken"/>.</param>
        /// <returns>The actual counter value after the increment. If the token is invalid, the current counter value is returned.</returns>
        public uint TryIncrement(uint token)
        {
            ulong state, newState;
            do
            {
                state = Volatile.Read(ref _state);
                uint currentGeneration = (uint)(state >> 32);
                if (currentGeneration != token)
                {
                    // the generation changed, so the counter was reset
                    // we can't increment the counter, since it's not the current generation
                    DebugLog.WriteDebug($"Ignoring invalid emptiness counter token {token}. Current token is {currentGeneration}.", LogWriter.Blocking);
                    return (uint)state;
                }
                // this is probably safe, if the counter ever overflows into the generation
                // then you definitely have bigger problems than a broken round robin qdisc :)
                newState = state + 1;
            } while (Interlocked.CompareExchange(ref _state, newState, state) != state);
            DebugLog.WriteDiagnostic($"Incremented emptiness counter to {newState & uint.MaxValue}.", LogWriter.Blocking);
            return (uint)newState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetToken() => (uint)(Volatile.Read(ref _state) >> 32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetCount() => (uint)Volatile.Read(ref _state);
    }
}

file readonly ref struct CriticalSection
{
    private readonly ref int _count;

    private CriticalSection(ref int state)
    {
        _count = ref state;
    }

    public static CriticalSection Enter(ref int state)
    {
        Interlocked.Increment(ref state);
        return new CriticalSection(ref state);
    }

    public void Exit() => Interlocked.Decrement(ref _count);

    public void SpinUntilEmpty()
    {
        SpinWait spinner = default;
        while (Interlocked.CompareExchange(ref _count, 0, 0) != 0)
        {
            spinner.SpinOnce();
        }
    }
}