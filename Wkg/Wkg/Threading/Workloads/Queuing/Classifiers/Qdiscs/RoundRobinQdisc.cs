using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Wkg.Common.Extensions;
using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers.Internals;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

namespace Wkg.Threading.Workloads.Queuing.Classifiers.Qdiscs;

/// <summary>
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
/// <typeparam name="TState">The type of the state.</typeparam>
public sealed class RoundRobinQdisc<THandle, TState> : ClassifyingQdisc<THandle, TState>, IClassifyingQdisc<THandle, TState, RoundRobinQdisc<THandle, TState>>
    where THandle : unmanaged
    where TState : class
{
    private readonly ThreadLocal<IQdisc?> _localLast;
    private readonly IClasslessQdisc<THandle> _localQueue;

    private volatile IChildClassification<THandle>[] _children;
    private readonly Predicate<TState> _predicate;
    private readonly ReaderWriterLockSlim _childrenLock;
    private int _rrIndex;
    private int _emptyCounter;
    private int _criticalDequeueSectionCounter;

    private RoundRobinQdisc(THandle handle, Predicate<TState> predicate) : base(handle)
    {
        _localQueue = FifoQdisc<THandle>.CreateAnonymous();
        _localLast = new ThreadLocal<IQdisc?>(trackAllValues: false);
        _children = new IChildClassification<THandle>[] { new NoChildClassification<THandle>(_localQueue) };
        _predicate = predicate;
        _childrenLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    /// <inheritdoc/>
    public static RoundRobinQdisc<THandle, TState> Create(THandle handle, Predicate<TState> predicate)
    {
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);
        return new RoundRobinQdisc<THandle, TState>(handle, predicate);
    }

    /// <inheritdoc/>
    public static RoundRobinQdisc<THandle, TState> CreateAnonymous(Predicate<TState> predicate) => 
        new(default, predicate);

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
                // or that new workloads are scheduled to children while we're counting them
                // dequeue operations are not a problem, since we only need to provide a weakly consistent count!
                _childrenLock.EnterReadLock();

                IChildClassification<THandle>[] children = _children;
                int count = 0;
                for (int i = 0; i < children.Length; i++)
                {
                    count += children[i].Qdisc.Count;
                }
                return count;
            }
            finally
            {
                _childrenLock.ExitReadLock();
            }
        }
    }

    private bool IsKnownEmptyVolatile
    {
        get
        {
            try
            {
                _childrenLock.EnterReadLock();
                // TODO: counter generations
                return Volatile.Read(ref _emptyCounter) >= _children.Length;
            }
            finally
            {
                _childrenLock.ExitReadLock();
            }
        }
    }

    /// <inheritdoc/>
    // not supported.
    // would only need to consider the local queue, since this
    // method is only called on the direct parent of a workload.
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    /// <inheritdoc/>
    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
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
        if (backTrack && _localLast.IsValueCreated && _localLast.Value?.TryDequeueInternal(backTrack, out workload) is true)
        {
            DebugLog.WriteDiagnostic($"{this} Backtracking to last child qdisc {_localLast.Value.GetType().Name} ({_localLast.Value}).", LogWriter.Blocking);
            return true;
        }
        // backtracking failed, or was not requested. We need to iterate over all child qdiscs.
        // we don't need a lock for dequeueing, since only empty qdiscs can be removed anyway
        // and new children being read an iteration later is not a problem. The problem only
        // arises when new workloads are scheduled to a stale child qdisc.
        // we do need to resample the children array on every iteration though, since it can change
        // while we're iterating over it.
        SpinWait spinner = default;

        // repeat until we successfully complete one full iteration over all child qdiscs
        // without the children array changing
        while (true)
        {
            // we need to keep track of the number of empty child qdiscs we encounter
            // we do this by incrementing the global empty counter on every iteration in which
            // any thread encounters an empty child qdisc. the counter is reset to 0 when a workload
            // is scheduled to any child qdisc, or whenever any thread encounters a non-empty child qdisc.
            // if the counter reaches the length of the children array, all child qdiscs are empty

            int emptyCounter = 0;
            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            do
            {
                // startIndex is not guaranteed to be in range, since we are reading *the original value*
                // the modulo is only applied after the increment, so we need to check if the index is in range
                int startIndex = Atomic.IncrementModulo(ref _rrIndex, children.Length);
                DebugLog.WriteDiagnostic($"{this} Round robin index: {startIndex}.", LogWriter.Blocking);
                if (startIndex >= children.Length)
                {
                    // we're out of range, the old round robin index was stale
                    // retry with the updated round robin index which is guaranteed to be in range
                    // (unless the array changed again, but that's unlikely)
                    DebugLog.WriteDiagnostic($"{this} Round robin index out of range, retrying with updated index.", LogWriter.Blocking);
                    continue;
                }
                // we're in range, try to dequeue from the child qdisc until we find a workload
                // or we wrap around and end up at the start index again
                // enter the critical dequeue section
                Interlocked.Increment(ref _criticalDequeueSectionCounter);
                IQdisc qdisc = children[startIndex].Qdisc;
                if (qdisc.TryDequeueInternal(backTrack, out workload))
                {
                    DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {qdisc}.", LogWriter.Blocking);
                    // we found a workload, update the last child qdisc and reset the empty counter
                    _localLast.Value = children[startIndex].Qdisc;
                    if (emptyCounter == children.Length)
                    {
                        DebugLog.WriteWarning($"{this} Dude, the greater than operator just saved you... you were about to lose a worker thread.", LogWriter.Blocking);
                    }
                    Interlocked.Exchange(ref _emptyCounter, 0);
                    // leave the critical dequeue section
                    Interlocked.Decrement(ref _criticalDequeueSectionCounter);
                    return true;
                }
                // we didn't find a workload, increment the empty counter
                // if the counter reaches the length of the children array, all child qdiscs are empty
                // and we can return false
                // TODO: this is the actual problem!!!
                // we increment the empty counter "from an old counter generation", so:
                // Thread 1: fails check at emptyCounter = length - 1
                // Thread 2: succeeds check with next index
                // Thread 2: resets empty counter to 0
                // Thread 1: increments empty counter based on outdated decision (the last read value was from Thread 2, which reset the counter)
                // this will cause the qdisc to be considered empty in the next round of iterations where
                // emptyCounter == children.Length, even though the very next child could return a workload
                // this race condition is NOT limited to 2 threads. so no number n will fix the broken check if (emptyCounter + n > children.Length)
                // TODO: threads should only be allowed to execute the line below if no reset occurred between sampling a child themselves and getting
                // to this line. this is a critical section that must be protected by a lock or with another atomic "generation" counter that is incremented
                // whenever the empty counter is reset. the generation counter must be sampled before the empty counter is incremented and compared to the
                // generation counter after the increment. if they are not equal, the increment must be discarded and the empty counter must be resampled.
                // this bug occurs in the transition from emptyCounter = 0 to emptyCounter > 0. (so right after the reset)
                // it manifests itself as a false positive for IsKnownEmptyVolatile, so when emptyCounter approaches children.Length!
                emptyCounter = Interlocked.Increment(ref _emptyCounter);
                // leave the critical dequeue section
                Interlocked.Decrement(ref _criticalDequeueSectionCounter);
                // we intentionally don't check for emptyCounter == children.Length here
                // we must re-sample the first element to be sure that everything is really empty
                // we give the other threads a chance to finish their work and update the empty counter
                if (emptyCounter > children.Length)
                {
                    // we meet requirements for worker termination
                    // *however* it's possible that one worker just took forever to dequeue a workload and that the 
                    // qdisc is not actually empty. We can't just return false here, since that would cause the worker
                    // to terminate permanently (until a new workload is scheduled). So we need to spin until we're sure
                    // that the qdisc is actually empty (wait for the critical dequeue section to be empty)
                    // use interlocked for a full fence (volatile could not be enough here)
                    DebugLog.WriteDiagnostic($"{this} All child qdiscs are empty, waiting for critical dequeue section to be empty.", LogWriter.Blocking);
                    // we need to do this to establish consensus with the other threads before we declare the qdisc as empty
                    // once they are declared empty, we will never check again until new workloads are scheduled. So this is
                    // kind of a big deal.
                    while (Interlocked.CompareExchange(ref _criticalDequeueSectionCounter, 0, 0) != 0)
                    {
                        spinner.SpinOnce();
                    }
                    DebugLog.WriteDiagnostic($"{this} Critical dequeue section is empty, resampling children.", LogWriter.Blocking);
                    // critical section is empty. RESAMPLE!
                    if (!IsKnownEmptyVolatile)
                    {
                        // the qdisc is not empty! continue with the next iteration
                        DebugLog.WriteDiagnostic($"{this} Qdisc is not empty, continuing with next iteration.", LogWriter.Blocking);
                        continue;
                    }
                    // well, we trid our best, but it seems like the qdisc is actually empty
                    // give up for now we must assume that the other threads did their checks
                    // correctly and that the qdisc is indeed empty
                    workload = null;
                    _localLast.Value = null;
                    DebugLog.WriteDiagnostic($"{this} Qdisc is empty, returning false.", LogWriter.Blocking);
                    return false;
                }
                // just try the next child qdisc
            }
            while (ReferenceEquals(children, Volatile.Read(ref _children)));
            DebugLog.WriteDebug($"{this} Children array changed while iterating over it, resampling children.", LogWriter.Blocking);
            // the children array changed while we were iterating over it
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
            _childrenLock.EnterReadLock();
            DebugLog.WriteDiagnostic($"Trying to enqueue workload {workload} to round robin qdisc {this}.", LogWriter.Blocking);

            IChildClassification<THandle>[] children = _children;
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
        if (state is TState typedState && _predicate(typedState))
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
    public override bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate)
    {
        IChildClassification<THandle> classifiedChild = new ChildClassification<THandle, TState>(child, predicate);
        return TryAddChildCore(classifiedChild);
    }

    /// <inheritdoc/>
    public override bool TryAddChild<TOtherState>(IClassifyingQdisc<THandle, TOtherState> child) where TOtherState : class
    {
        IChildClassification<THandle> classifiedChild = new ClassifierChildClassification<THandle, TOtherState>(child);
        return TryAddChildCore(classifiedChild);
    }

    private bool TryAddChildCore(IChildClassification<THandle> child)
    {
        DebugLog.WriteDiagnostic($"Trying to add child qdisc {child.Qdisc} to round robin qdisc {this}.", LogWriter.Blocking);
        // link the child qdisc to the parent qdisc first
        child.Qdisc.InternalInitialize(this);

        try
        {
            _childrenLock.EnterWriteLock();

            // get the latest children array
            IChildClassification<THandle>[] children = _children;

            // it is possible that the child was already added previously, so we need to check for that
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
            IChildClassification<THandle>[] newChildren = new IChildClassification<THandle>[children.Length + 1];
            Array.Copy(children, newChildren, children.Length);
            newChildren[^1] = child;

            // use Volatile.Write to ensure that the new array is visible to other threads and not just written to the local cache
            _children = newChildren;
            DebugLog.WriteDiagnostic($"Added child qdisc {child.Qdisc} to round robin qdisc {this}.", LogWriter.Blocking);
            return true;
        }
        finally
        {
            _childrenLock.ExitWriteLock();
        }
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
        if (!this.To<IClassfulQdisc<THandle>>().ContainsChild(in child.Handle))
        {
            DebugLog.WriteDiagnostic($"Quickly returning false, child qdisc {child} not present in round robin qdisc {this}.", LogWriter.Blocking);
            return false;
        }
        try
        {
            // that's unfortunate, we need to acquire the write lock
            _childrenLock.EnterWriteLock();

            IChildClassification<THandle>[] children = _children;
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
    protected override bool ContainsChild(in THandle handle) =>
        TryFindChild(handle, out _);

    /// <inheritdoc/>
    protected override bool TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child)
    {
        try
        {
            _childrenLock.EnterReadLock();

            IChildClassification<THandle>[] children = _children;
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

    /// <inheritdoc/>
    protected override void OnWorkScheduled()
    {
        // we must reset the empty counter before we call the parent scheduler
        // we need a full fence here, since we need to ensure that the empty counter is reset
        // before the parent scheduler is notified (Volatile.Write has release semantics)
        Interlocked.Exchange(ref _emptyCounter, 0);
        DebugLog.WriteDiagnostic($"{this}: reset empty counter to 0.", LogWriter.Blocking);
        base.OnWorkScheduled();
    }
}
