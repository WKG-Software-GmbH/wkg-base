using System.Diagnostics;
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
    private readonly EmptyCounter _emptyCounter;
    private int _rrIndex;
    private int _criticalDequeueSection;

    private RoundRobinQdisc(THandle handle, Predicate<TState> predicate) : base(handle)
    {
        _localQueue = FifoQdisc<THandle>.CreateAnonymous();
        _localLast = new ThreadLocal<IQdisc?>(trackAllValues: false);
        _children = new IChildClassification<THandle>[] { new NoChildClassification<THandle>(_localQueue) };
        _predicate = predicate;
        _childrenLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        _emptyCounter = new EmptyCounter();
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
                return _emptyCounter.GetCount() >= _children.Length;
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
        try
        {
            // TODO: would be cool to do a try enter read lock here, and do other stuff if we can't
            // makes no sense to block all workers just because someone is modifying the children array
            // we can't also just try and return false, since our parent may think that we're empty
            _childrenLock.EnterReadLock();

            IChildClassification<THandle>[] children = _children;

            // loop until we find a workload or until we meet requirements for worker termination
            while (true) 
            {
                // this is our local round robin index. it is guaranteed to be in range and that we are the only thread
                // that has this index assigned (well, unless there's more worker threads than children, but that's unlikely)
                // NOTE: that the _rrIndex is *post-incremented*, so our index is _rrIndex - 1
                // (well it would be, if it wasn't for the modulo)
                int index = Atomic.IncrementModulo(ref _rrIndex, children.Length);
                uint token = _emptyCounter.GetToken();
                CriticalSection criticalSection = CriticalSection.Enter(ref _criticalDequeueSection);
                IQdisc qdisc = children[index].Qdisc;
                if (qdisc.TryDequeueInternal(backTrack, out workload))
                {
                    DebugLog.WriteDiagnostic($"{this} Dequeued workload from child qdisc {qdisc}.", LogWriter.Blocking);
                    // we found a workload, update the last child qdisc and reset the empty counter
                    _localLast.Value = children[index].Qdisc;
                    // reset the empty counter and start a new counter generation *before* we leave the critical dequeue section
                    _emptyCounter.Reset();
                    // leave the critical dequeue section
                    criticalSection.Exit();
                    return true;
                }
                // leave the critical dequeue section
                criticalSection.Exit();
                // we didn't find a workload, increment the empty counter
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
                        _localLast.Value = null;
                        DebugLog.WriteDiagnostic($"{this} Qdisc is empty, returning false.", LogWriter.Blocking);
                        return false;
                    }

                    // the qdisc is not empty! continue with the next iteration
                    DebugLog.WriteDiagnostic($"{this} Qdisc is not empty, continuing with next iteration.", LogWriter.Blocking);
                }
                // the qdisc is not empty, but we didn't find a workload. continue with the next iteration
            }
        }
        finally
        {
            _childrenLock.ExitReadLock();
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

        public uint GetToken() => (uint)(Volatile.Read(ref _state) >> 32);

        public uint GetCount() => (uint)Volatile.Read(ref _state);
    }

    private readonly ref struct CriticalSection
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
}

