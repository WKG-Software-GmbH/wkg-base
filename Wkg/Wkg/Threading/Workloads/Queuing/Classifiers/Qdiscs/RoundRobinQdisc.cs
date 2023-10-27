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

internal class RoundRobinQdisc<THandle, TState> : Qdisc<THandle>, IClassifyingQdisc<THandle, TState, RoundRobinQdisc<THandle, TState>>
    where THandle : unmanaged
    where TState : class
{
    private readonly ThreadLocal<IQdisc?> _localLast;
    private readonly FifoQdisc<THandle> _localQueue;

    /// <summary>
    /// The children array is immutable by contract, so we can read it without locking.
    /// If children need to be added or removed, a new array is created and the reference is CASed in.
    /// For removing children, the write lock must be acquired, so that the child qdisc can be removed safely.
    /// </summary>
    private IChildClassification<THandle>[] _children;
    private readonly Predicate<TState> _predicate;
    private readonly ReaderWriterLockSlim _childrenLock;
    private int _rrIndex;
    private int _emptyCounter;
    private int _criticalDequeueSectionCounter;

    internal RoundRobinQdisc(THandle handle, Predicate<TState> predicate) : base(handle)
    {
        _localQueue = new FifoQdisc<THandle>(default);
        _localLast = new ThreadLocal<IQdisc?>(trackAllValues: false);
        _children = new IChildClassification<THandle>[] { new NoChildClassification<THandle>(_localQueue) };
        _predicate = predicate;
        _childrenLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
    }

    public static RoundRobinQdisc<THandle, TState> Create(THandle handle, Predicate<TState> predicate)
    {
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);
        return new RoundRobinQdisc<THandle, TState>(handle, predicate);
    }

    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) => 
        // TODO: probably should allow a public way to do this to make the API extendable
        _localQueue.To<IQdisc>().InternalInitialize(this);

    public override bool IsEmpty => Count == 0;

    public override int Count
    {
        get
        {
            try
            {
                // we don't actually write to the children array, but we must ensure that no other thread is removing children
                // or that new workloads are scheduled to children while we're counting them
                // dequeue operations are not a problem, since we only need to provide a weakly consistent count!
                _childrenLock.EnterWriteLock();
                // get a local snapshot of the children array, other threads may still add new children which we don't care about here
                IChildClassification<THandle>[] children = Volatile.Read(ref _children);
                int count = 0;
                for (int i = 0; i < children.Length; i++)
                {
                    count += children[i].Qdisc.Count;
                }
                return count;
            }
            finally
            {
                _childrenLock.ExitWriteLock();
            }
        }
    }

    // would only need to consider the local queue, since this
    // method is only called on the direct parent of a workload.
    protected override bool TryRemoveInternal(CancelableWorkload workload) => false;

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // if we have to backtrack, we can do so by dequeuing from the last child qdisc
        // that was dequeued from. If the last child qdisc is empty, we can't backtrack and continue
        // with the next child qdisc.
        if (backTrack && _localLast.IsValueCreated && _localLast.Value?.TryDequeueInternal(backTrack, out workload) is true)
        {
            DebugLog.WriteDiagnostic($"Backtracking to last child qdisc {_localLast.Value.GetType().Name} ({_localLast.Value}).", LogWriter.Blocking);
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
            // we also initialize our local empty counter to 0 (regardless of the global empty counter)
            // in order to allow us to recover after after all child qdiscs were empty at one point
            // if everything really is empty, we can return false and the worker will exit
            // also if the children array changes while we're iterating over it, we need to start over and 
            // resample all children for emptiness again

            // TODO: without the following like, we will sometimes miss child qdiscs that are not empty, causing
            // worker termination and orphaned workloads!
            // it is a good idea for every dequeue operation to reset this empty counter. If children are really
            // empty, workers will aggregate here until they are 100% sure that the qdisc is empty.
            // this obviously sucks for performance, but it's better than orphaned workloads.
            // we can instead introduce an additional "confirmed empty" flag that is set once after workers have
            // established that the qdisc is empty. This flag can be reset whenever a new workload is scheduled to 
            // this qdisc or any of its children. This way, we can avoid the expensive empty check in most cases
            // and still be sure that we don't miss any workloads.
            Volatile.Write(ref _emptyCounter, 0);

            int emptyCounter;
            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            do
            {
                // startIndex is not guaranteed to be in range, since we are reading *the original value*
                // the modulo is only applied after the increment, so we need to check if the index is in range
                int startIndex = Atomic.IncrementModulo(ref _rrIndex, children.Length);
                DebugLog.WriteDiagnostic($"Round robin index: {startIndex}.", LogWriter.Blocking);
                if (startIndex < children.Length)
                {
                    // we're in range, try to dequeue from the child qdisc until we find a workload
                    // or we wrap around and end up at the start index again
                    // enter the critical dequeue section
                    Interlocked.Increment(ref _criticalDequeueSectionCounter);
                    IQdisc qdisc = children[startIndex].Qdisc;
                    if (qdisc.TryDequeueInternal(backTrack, out workload))
                    {
                        DebugLog.WriteDiagnostic($"Dequeued workload from child qdisc {qdisc.GetType().Name} ({qdisc}).", LogWriter.Blocking);
                        // we found a workload, update the last child qdisc and reset the empty counter
                        _localLast.Value = children[startIndex].Qdisc;
                        Volatile.Write(ref _emptyCounter, 0);
                        // leave the critical dequeue section
                        Interlocked.Decrement(ref _criticalDequeueSectionCounter);
                        return true;
                    }
                    // we didn't find a workload, increment the empty counter
                    // if the counter reaches the length of the children array, all child qdiscs are empty
                    // and we can return false
                    emptyCounter = Interlocked.Increment(ref _emptyCounter);
                    // leave the critical dequeue section
                    Interlocked.Decrement(ref _criticalDequeueSectionCounter);
                }
                else
                {
                    // we're out of range, the old round robin index was stale
                    // retry with the updated round robin index which is guaranteed to be in range
                    // (unless the array changed again, but that's unlikely)
                    DebugLog.WriteDiagnostic($"Round robin index out of range, retrying with updated index.", LogWriter.Blocking);
                    spinner.SpinOnce();
                    continue;
                }
                // we intentionally don't check for emptyCounter == children.Length here
                // we give the other threads a chance to finish their work and update the empty counter
                if (emptyCounter >= children.Length)
                {
                    // we meet requirements for worker termination
                    // *however* it's possible that one worker just took forever to dequeue a workload and that the 
                    // qdisc is not actually empty. We can't just return false here, since that would cause the worker
                    // to terminate permanently (until a new workload is scheduled). So we need to spin until we're sure
                    // that the qdisc is actually empty (wait for the critical dequeue section to be empty)
                    // use interlocked for a full fence (volatile could not be enough here)
                    DebugLog.WriteDiagnostic($"All child qdiscs are empty, waiting for critical dequeue section to be empty.", LogWriter.Blocking);
                    while (Interlocked.CompareExchange(ref _criticalDequeueSectionCounter, 0, 0) != 0)
                    {
                        spinner.SpinOnce();
                    }
                    DebugLog.WriteDiagnostic($"Critical dequeue section is empty, resampling children.", LogWriter.Blocking);
                    // critical section is empty. RESAMPLE!
                    emptyCounter = Volatile.Read(ref _emptyCounter);
                    if (emptyCounter < children.Length)
                    {
                        // the qdisc is not empty! continue with the next iteration
                        DebugLog.WriteDiagnostic($"Qdisc is not empty, continuing with next iteration.", LogWriter.Blocking);
                        continue;
                    }
                    // well, we trid our best, but it seems like the qdisc is actually empty
                    // give up for now we must assume that the other threads did their checks
                    // correctly and that the qdisc is indeed empty
                    workload = null;
                    _localLast.Value = null;
                    DebugLog.WriteDiagnostic($"Qdisc is empty, returning false.", LogWriter.Blocking);
                    return false;
                }
                // just try the next child qdisc
            }
            while (ReferenceEquals(children, Volatile.Read(ref _children)));
            DebugLog.WriteDebug($"Children array changed while iterating over it, resampling children.", LogWriter.Blocking);
            // the children array changed while we were iterating over it
        }
    }

    public bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        try
        {
            // prevent children from being removed while we're iterating over them
            _childrenLock.EnterReadLock();
            DebugLog.WriteDiagnostic($"Trying to enqueue workload {workload} to round robin qdisc {this}.", LogWriter.Blocking);

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].TryEnqueue(state, workload))
                {
                    DebugLog.WriteDiagnostic($"Enqueued workload {workload} to child qdisc {children[i].Qdisc}.", LogWriter.Blocking);
                    // TODO: rethink this!
                    // we found a child qdisc that accepted the workload, reset the empty counter (we have work to do!)
                    Volatile.Write(ref _emptyCounter, 0);
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

    public bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (state is TState typedState && _predicate(typedState))
        {
            DebugLog.WriteDiagnostic($"Enqueuing workload {workload} directly to round robin qdisc {this}.", LogWriter.Blocking);
            Enqueue(workload);
            return true;
        }
        DebugLog.WriteDiagnostic($"Could not enqueue workload {workload} directly to round robin qdisc {this}.", LogWriter.Blocking);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enqueue(AbstractWorkloadBase workload)
    {
        // TODO: rethink this!
        // we are about to enqueue a workload directly to the local queue
        // reset the empty counter (we have work to do!)
        Volatile.Write(ref _emptyCounter, 0);
        _localQueue.Enqueue(workload);
    }

    public bool TryAddChild(IClasslessQdisc<THandle> child)
    {
        IChildClassification<THandle> classifiedChild = new NoChildClassification<THandle>(child);
        return TryAddChildCore(classifiedChild);
    }

    public bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<TState> predicate)
    {
        IChildClassification<THandle> classifiedChild = new ChildClassification<THandle, TState>(child, predicate);
        return TryAddChildCore(classifiedChild);
    }

    public bool TryAddChild<TOtherState>(IClassifyingQdisc<THandle, TOtherState> child) where TOtherState : class
    {
        IChildClassification<THandle> classifiedChild = new ClassifierChildClassification<THandle, TOtherState>(child);
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

    public bool TryRemoveChild(IClasslessQdisc<THandle> child) =>
        RemoveChildInternal(child, -1);

    public bool RemoveChild(IClasslessQdisc<THandle> child) =>
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

            if (!this.To<IClassfulQdisc<THandle>>().ContainsChild(in child.Handle))
            {
                // child not present anymore
                DebugLog.WriteDiagnostic($"Returning false, child was present in round robin qdisc {this} when we checked, but not anymore.", LogWriter.Blocking);
                return false;
            }
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

            // the fact that we have the write lock means that no other thread can schedule work to 
            IChildClassification<THandle>[] children, newChildren;
            do
            {
                // get local readonly snapshot of the children array
                children = Volatile.Read(ref _children);
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
                newChildren = new IChildClassification<THandle>[children.Length - 1];
                children.AsSpan(0, childIndex).CopyTo(newChildren);
                children.AsSpan(childIndex + 1).CopyTo(newChildren.AsSpan(childIndex));
                // try to CAS the new array in, if it fails, try again
            } while (Interlocked.CompareExchange(ref _children, newChildren, children) != children);
            // CAS succeeded, we're done
            DebugLog.WriteDiagnostic($"Removed child qdisc {child} from round robin qdisc {this}.", LogWriter.Blocking);
            return true;
        }
        finally
        {
            _childrenLock.ExitWriteLock();
        }
    }

    bool IClassfulQdisc<THandle>.ContainsChild(in THandle handle) =>
        this.To<IClassfulQdisc<THandle>>().TryFindChild(handle, out _);

    bool IClassfulQdisc<THandle>.TryFindChild(in THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child)
    {
        // no lock needed, if a new child is added, a new array is created and the reference is CASed in
        // we could get a stale snapshot of the children array, but that's perfectly fine. We can't guarantee
        // that another thread does something with the child qdisc just right after we found it, so we only need
        // to ensure that the collection is stable while we're iterating over it.
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

    void INotifyWorkScheduled.OnWorkScheduled() => ParentScheduler.OnWorkScheduled();
}
