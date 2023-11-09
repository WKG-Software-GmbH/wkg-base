using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection.PortableExecutable;
using Wkg.Collections.Concurrent;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Extensions;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;
using Wkg.Threading.Workloads.Queuing.Classful.Routing;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.VirtualTime;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

// TODO: maybe rename this qdisc to something more generic, as it is just a time-based fair scheduler
// actually make the time-based calculations configurable tho
internal class EarliestDueDateQdisc<THandle> : ClassfulQdisc<THandle> where THandle : unmanaged
{
    [ThreadStatic]
    private static int? __LAST_ENQUEUED_CHILD_INDEX;

    private readonly IVirtualTimeTable _timeTable;
    private readonly ReaderWriterLockSlim _childModificationLock = new();
    private readonly ReaderWriterLockSlim _schedulerLock = new();
    private readonly IClasslessQdisc<THandle> _localQueue;
    private readonly Predicate<object?> _predicate;

    private uint _generationCounter;
    private ConcurrentBitMap64OLD _isEmptyMap;
    private volatile ChildQdiscState[] _childStates;

    public EarliestDueDateQdisc(THandle handle, Predicate<object?> predicate, IClasslessQdiscBuilder inner, int concurrencyLevel) : base(handle)
    {
        _timeTable = VirtualTimeTable.CreatePrecise(concurrencyLevel, 32);
        _predicate = predicate;
        _localQueue = inner.BuildUnsafe(default(THandle));
        _childStates = new ChildQdiscState[1] { new ChildQdiscState(new NoChildClassification<THandle>(_localQueue)) };
        _predicate = predicate;
        _isEmptyMap = default;
    }
    
    /// <inheritdoc/>
    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    public override int Count
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }
            // we can run concurrently to dequeue operations, so no need to get an exclusive lock
            using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
            // however, we need to ensure that no additional workloads are enqueued while we are counting
            using ILockOwnership exclusiveSchedulerLock = _schedulerLock.AcquireWriteLock();
            int count = 0;
            ChildQdiscState[] childStates = _childStates;
            for (int i = 0; i < childStates.Length; i++)
            {
                count += childStates[i].Child.Qdisc.Count;
            }
            // don't forget the local caches
            // just pop-count all the bits that are not set
            ulong emptyBitMap = _isEmptyMap.AsUInt64();
            // for that we need to invert the bit map, so that empty children are 1 and non-empty children are 0
            // we then need to mask out all the bits that are not used but are purely padding
            return count + BitOperations.PopCount(~emptyBitMap & ((1uL << childStates.Length) - 1));
        }
    }

    public override bool IsEmpty
    {
        get
        {
            using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
            return IsKnownEmptyVolatileUnsafe;
        }
    }

    private bool IsKnownEmptyVolatileUnsafe
    {
        get
        {
            ChildQdiscState[] childStates = _childStates;
            ConcurrentBitMap64OLD emptyMap = ConcurrentBitMap64OLD.VolatileRead(ref _isEmptyMap);
            return emptyMap.IsFull(childStates.Length);
        }
    }

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        using ILockOwnership childStateReadLock = _childModificationLock.AcquireReadLock();
        if (IsKnownEmptyVolatileUnsafe)
        {
            // short path for cheap early exit
            DebugLog.WriteDiagnostic($"{this}: qdisc is known to be empty. Taking fast path and exiting early.", LogWriter.Blocking);
            workload = null;
            return false;
        }

        DebugLog.WriteDiagnostic($"{this}: qdisc is not known to be empty. Taking slow path.", LogWriter.Blocking);

        SpinWait spinner = default;
        ChildQdiscState[] childStates = _childStates;

        // we need to keep trying until we either find a candidate or we know that the qdisc is empty
        // if we find a candidate, we return immediately
        while (!IsKnownEmptyVolatileUnsafe)
        {
            if (TryFindBestCandidateUnsafe(childStates, workerId, out _, out workload) && workload is not null)
            {
                return true;
            }
            // we didn't find a candidate.
            // either all children are empty
            // OR we skipped a child previously because it was being repopulated
            // wait for a bit and try again
            DebugLog.WriteDiagnostic($"{this}: failed to find a candidate. Retrying after spin wait.", LogWriter.Blocking);
            spinner.SpinOnce();
        }
        DebugLog.WriteDiagnostic($"{this}: failed to peek a workload. Children are empty.", LogWriter.Blocking);
        // all children are empty
        workload = null;
        return false;
    }

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        using ILockOwnership childStateReadLock = _childModificationLock.AcquireReadLock();

        if (IsKnownEmptyVolatileUnsafe)
        {
            // short path for cheap early exit
            DebugLog.WriteDiagnostic($"{this}: qdisc is known to be empty. Taking fast path and exiting early.", LogWriter.Blocking);
            workload = null;
            return false;
        }

        DebugLog.WriteDiagnostic($"{this}: qdisc is not known to be empty. Taking slow path.", LogWriter.Blocking);

        SpinWait spinner = default;
        ChildQdiscState[] childStates = _childStates;

        // we need to keep trying until we either find a candidate or we know that the qdisc is empty
        // if we find a candidate, we return immediately
        while (!IsKnownEmptyVolatileUnsafe)
        {
            if (TryFindBestCandidateUnsafe(childStates, workerId, out int candidateIndex, out AbstractWorkloadBase? candidate) && candidate is not null)
            {
                ChildQdiscState child = childStates[candidateIndex];
                // we found a candidate, attempt to dequeue it
                lock (child.QdiscLock)
                {
                    // We now hold the lock on the child qdisc.
                    // this can properly just be a volatile write and normal compare, as we hold the lock on the child qdisc
                    if (Interlocked.CompareExchange(ref child.CandidateRef, null, candidate) == candidate)
                    {
                        // we successfully dequeued the candidate
                        // repopulate the candidate buffer
                        _ = TryRepopulateCandidateUnsafe(child, workerId, candidateIndex, out _);
                        // in either case, we now have a workload to return
                        workload = candidate;
                        // update the virtual time
                        EarliestDueDateState state = (EarliestDueDateState)workload._state!;
                        DebugLog.WriteDiagnostic($"{this}: scheduling workload {workload} with virtual execution time {state.VirtualExecutionTime} and expected execution time {state.ExpectedExecutionTime}.", LogWriter.Blocking);
                        // we assume average execution time for the aggregate
                        // but we assume worst case execution time for the workload itself
                        // this can be tweaked for different scheduling policies
                        // we are the only ones able to update the virtual finish time (we already hold the lock on the child qdisc)
                        // TODO: we can tweak the virtual finish time calculation to allow fair scheduling in regard to the entire history of the scheduler
                        // (i.e., just increment the virtual finish time by the average execution time of the workload)
                        // OR we can tweak it to allow fair scheduling in regard to the most recent history of the scheduler
                        // (i.e., we reset the virtual finish time to the current time and increment it by the average execution time of the workload)
                        double lastVirtualFinishTime = _timeTable.Now() + state.ExpectedExecutionTime;
                        Volatile.Write(ref child.LastVirtualFinishTimeRef, lastVirtualFinishTime);
                        // we just changed the virtual finish time of a child, so we need to increment the generation counter
                        Interlocked.Increment(ref _generationCounter);
                        // don't forget to strip our state from the workload
                        workload._state = state.Strip();
                        // start the next execution time measurement
                        _timeTable.StartMeasurement(workload);
                        return true;
                    }
                    // someone else dequeued the candidate before us, so we need to restart the calculation
                }
            }
            // we didn't find a candidate.
            // either all children are empty
            // OR we skipped a child previously because it was being repopulated
            // wait for a bit and try again
            DebugLog.WriteDiagnostic($"{this}: failed to find a candidate. Retrying after spin wait.", LogWriter.Blocking);
            spinner.SpinOnce();
        }
        DebugLog.WriteDiagnostic($"{this}: failed to dequeue a workload. Children are empty.", LogWriter.Blocking);
        // all children are empty
        workload = null;
        return false;
    }

    private bool TryFindBestCandidateUnsafe(ChildQdiscState[] childStates, int workerId, out int candidateIndex, out AbstractWorkloadBase? candidate)
    {
        // REQUIRES: read lock on _childModificationLock
        // take a snapshot of the empty map
        ConcurrentBitMap64OLD emptinessMap = ConcurrentBitMap64OLD.VolatileRead(ref _isEmptyMap);
        // prepare local variables for the candidate search
        AbstractWorkloadBase? currentCandidate = null;
        candidateIndex = -1;
        // we search for the minimum due date
        double earliestDueDate = double.PositiveInfinity;
        // if this generation counter changes while we are iterating over the children, we need to restart the calculation
        // this is because the candidate buffer may have changed
        uint generationCounter = Volatile.Read(ref _generationCounter);
        // we need to keep track of the original generation counter, so we can detect changes
        uint originalGenerationCounter = generationCounter;
        // loop over all children or until the generation counter changes
        for (int i = 0; i < childStates.Length && generationCounter == originalGenerationCounter; i++, generationCounter = Volatile.Read(ref _generationCounter))
        {
            // keep track of empty children in an atomic fashion, children that are known to be empty can be skipped
            // if someone changes the emptiness map, they will also update the generation counter
            // so we can operate on the assumption that the emptiness map is consistent with the generation counter
            // meaning we don't need to re-sample the emptiness map every iteration
            if (emptinessMap.IsBitSet(i))
            {
                // skip empty children
                continue;
            }
            // found a potentially non-empty child (we don't know for sure yet)
            ChildQdiscState childState = childStates[i];
            // get the next candidate from the child.
            // this is basically a peek operation
            // we keep candidates in a special property of the child qdisc state to avoid having to call into the child qdisc itself
            // this is for one a form of optimization, but we also don't know how deterministic the child qdisc is, so we just buffer
            // the would-be-peeked workload in the child qdisc state
            AbstractWorkloadBase? possibleCandidate = Volatile.Read(ref childState.CandidateRef);
            // the candidate is not guaranteed to be non-null, as enqueue events only reset the bit in the emptiness map but don't update the candidate buffer
            if (possibleCandidate is null)
            {
                DebugLog.WriteDiagnostic($"{this}: candidate buffer for child {childState.Child.Qdisc} is empty, but bit map indicates that it is not empty. Attempting to repopulate the candidate buffer.", LogWriter.Blocking);
                // there are two reasons why the candidate buffer can be empty:
                // 1. a workload was just enqueued to the child and we are the first worker to notice that the cache wasn't loaded yet
                // 2. our local emptiness map is out of date and the child is actually empty
                // in any case, we need to acquire the child qdisc lock to determine what's going on here
                // we only do a try enter though, as, if another worker is currently repopulating the candidate buffer, we can check back on it later.
                // we only want to ensure that the candidate buffer is repopulated at all, not that we are the ones doing it.
                if (Monitor.TryEnter(childState.QdiscLock))
                {
                    // re-sample the emptiness map to check if the child is actually empty
                    emptinessMap = ConcurrentBitMap64OLD.VolatileRead(ref _isEmptyMap);
                    // re-sample the candidate buffer as well, as it may have changed between our last check and us acquiring the lock
                    possibleCandidate = Volatile.Read(ref childState.CandidateRef);
                    if (possibleCandidate is not null)
                    {
                        // the candidate buffer was repopulated before we got to the TryEnter call
                        // this is a totally fine but rare scenario, as we only need to ensure that the candidate buffer is repopulated
                        // we can just continue as normal
                        Debug.Assert(!emptinessMap.IsBitSet(i));
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: candidate buffer for child {childState.Child.Qdisc} was repopulated before we got to the TryEnter call. Continuing as normal.", LogWriter.Blocking);
                    }
                    // try to populate if: the empty bit is not set and the candidate buffer is still empty
                    else if (!emptinessMap.IsBitSet(i) && TryRepopulateCandidateUnsafe(childState, workerId, i, out possibleCandidate))
                    {
                        // the child is not empty anymore, we successfully repopulated the candidate buffer
                        // continue as normal
                        originalGenerationCounter = Interlocked.Increment(ref _generationCounter);
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: successfully repopulated candidate buffer for child {childState.Child.Qdisc}. Generation counter is now {originalGenerationCounter}.", LogWriter.Blocking);
                    }
                    else
                    {
                        // the child became empty while since our last check
                        // or we ourselves determined that the child is empty
                        // in any case, we can skip this child
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: child {childState.Child.Qdisc} is empty. Skipping.", LogWriter.Blocking);
                        continue;
                    }
                }
                else
                {
                    // someone else is repopulating this child, skip it for now and eventually check back later if we don't find another good candidate.
                    // this may seem to introduce unfairness, but that depends on the definition.
                    // the other worker runs in parallel to us, so the fact that we focus on a different workload doesn't mean that we are unfair.
                    // if the workload that is being repopulated right now is the best candidate, then it will be scheduled by the worker that is currently repopulating it.
                    // as a matter of fact, doing it this way is actually fairer than us waiting for the other worker to finish, because the other worker can immediately
                    // continue the earliest due date evaluation while we are still stuck in the lock and are waiting for the CLR to unblock us.
                    // this doesn't guarantee that the workload will actually run before whatever second best candidate we find, but it significantly increases the chances
                    // and it is faster for us to just continue anyway, as we don't need to wait for the lock to be released.
                    DebugLog.WriteDiagnostic($"{this}: failed to acquire child qdisc lock for child {childState.Child.Qdisc}. Skipping for now.", LogWriter.Blocking);
                    continue;
                }
            }
            // we actually found a non-null candidate.
            if (possibleCandidate._state is not EarliestDueDateState candidateState)
            {
                // this should never happen, as we only enqueue workloads with an earliest due date state
                // before we can abort the workload, we must acquire the child qdisc lock
                // this is to ensure that the workload doesn't get cancelled multiple times by different workers.
                lock (childState.QdiscLock)
                {
                    // we now hold the lock on the child qdisc.
                    // this can properly just be a volatile write and normal compare, as we hold the lock on the child qdisc
                    if (ReferenceEquals(Volatile.Read(ref childState.CandidateRef), possibleCandidate))
                    {
                        // we successfully dequeued the candidate and can now abort it
                        // repopulate the candidate buffer
                        if (TryRepopulateCandidateUnsafe(childState, workerId, i, out possibleCandidate))
                        {
                            // we successfully repopulated the candidate buffer, increment the generation counter
                            originalGenerationCounter = Interlocked.Increment(ref _generationCounter);
                        }
                    }
                    else
                    {
                        // someone else dequeued the candidate before us, so we need to restart the calculation
                        break;
                    }
                }
                // we have all the time in the world to abort the workload, so we don't need to do it in the lock
                WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: workload {possibleCandidate} has no {nameof(EarliestDueDateState)}.");
                Debug.Fail(exception.Message);
                DebugLog.WriteException(exception, LogWriter.Blocking);
                // in this case, we must abort the workload, as we can't properly handle it
                possibleCandidate.InternalAbort(exception);
                // technically we don't need to restart the calculation, as we (maybe) still hold a valid generation counter
                // so just try again for the current child qdisc
                i--;
                continue;
            }
            // we found a valid candidate, determine the earliest due date
            double virtualFinishTime = Volatile.Read(ref childState.LastVirtualFinishTimeRef) + candidateState.VirtualExecutionTime;
            if (virtualFinishTime < earliestDueDate)
            {
                earliestDueDate = virtualFinishTime;
                currentCandidate = possibleCandidate;
                candidateIndex = i;
            }
        }
        if (generationCounter != originalGenerationCounter)
        {
            // the generation counter changed while we were iterating over the children
            // this means that the candidate buffer may have changed, so we need to restart the calculation
            DebugLog.WriteDiagnostic($"{this}: generation counter changed while iterating over children. Restarting calculation.", LogWriter.Blocking);
            candidate = null;
            return false;
        }
        candidate = currentCandidate;
        return true;
    }

    private bool TryRepopulateCandidateUnsafe(ChildQdiscState child, int workerId, int childIndex, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        // before calling this method, the caller must own the following locks:
        // _childModificationLock
        // child.QdiscLock
        Debug.Assert(child.CandidateRef == null);
        DebugLog.WriteDiagnostic($"{this}: attempting to repopulate candidate buffer for child {child.Child.Qdisc} ({childIndex}).", LogWriter.Blocking);
        if (child.Child.Qdisc.TryDequeueInternal(workerId, false, out workload))
        {
            // we successfully dequeued a workload from the child
            Volatile.Write(ref child.CandidateRef, workload);
            DebugLog.WriteDiagnostic($"{this}: successfully repopulated candidate buffer for child {child.Child.Qdisc} ({childIndex}).", LogWriter.Blocking);
            return true;
        }
        // the child is empty
        // mark it as empty
        DebugLog.WriteDiagnostic($"{this}: child {child.Child.Qdisc} ({childIndex}) is empty. Updating emptiness bit map.", LogWriter.Blocking);
        ConcurrentBitMap64OLD.UpdateBit(ref _isEmptyMap, childIndex, isSet: true);
        return false;
    }

    protected override bool CanClassify(object? state)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();

        // snap a local copy of the children
        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            if (childStates[i].Child.CanClassify(state))
            {
                return true;
            }
        }
        return false;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();

        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            ChildQdiscState childState = childStates[i];
            if (childState.Child.CanClassify(state))
            {
                UpdateWorkloadState(workload);
                // lock to prevent lost updates to the emptiness tracking
                // TODO: this should be reworked to be atomic with a generation counter
                // TODO: create a *real* concurrent bit map
                lock (childState.QdiscLock)
                {
                    // update the emptiness tracking
                    // the actual reset happens in the OnWorkScheduled callback, but we need to
                    // set up the index of the child that was just enqueued to for that
                    __LAST_ENQUEUED_CHILD_INDEX = i;
                    if (!childState.Child.TryEnqueue(state, workload))
                    {
                        // this should never happen, as we already checked if the child can classify the workload
                        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {childStates[i].Child.Qdisc} reported to be able to classify workload {workload}, but failed to do so.");
                        Debug.Fail(exception.Message);
                        DebugLog.WriteException(exception, LogWriter.Blocking);
                        // we are on the enqueueing thread, so we can just throw here
                        throw exception;
                    }
                }
                return true;
            }
        }
        return false;
    }

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();

        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            ChildQdiscState childState = childStates[i];
            if (childState.Child.Qdisc.Handle.Equals(handle))
            {
                path.Add(new RoutingPathNode<THandle>(this, handle, i));
                path.Complete(childState.Child.Qdisc);
                return true;
            }
            if (childState.Child is IClassfulQdisc<THandle> classfulChild && classfulChild.TryFindRoute(handle, ref path))
            {
                path.Add(new RoutingPathNode<THandle>(this, handle, i));
                return true;
            }
        }
        return false;
    }

    protected override void WillEnqueueFromRoutingPath(ref RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        ChildQdiscState[] childStates = _childStates;

        int index = routingPathNode.Offset;
        ChildQdiscState? childState = null;

        if (index < childStates.Length && childStates[index].Child.Qdisc.Handle.Equals(routingPathNode.Handle))
        {
            // fast path. the cached offset is still valid
            childState = childStates[index];
        }
        else
        {
            // slow path. the cached offset is no longer valid
            for (int i = 0; i < childStates.Length; i++)
            {
                if (childStates[i].Child.Qdisc.Handle.Equals(routingPathNode.Handle))
                {
                    index = i;
                    childState = childStates[i];
                    break;
                }
            }
            if (childState is null)
            {
                // the child is no longer part of the qdisc
                // int this case we can't do anything, as we don't know where to enqueue the workload
                WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {routingPathNode.Handle} is no longer part of the qdisc.");
                Debug.Fail(exception.Message);
                DebugLog.WriteException(exception, LogWriter.Blocking);
                // we are on the enqueueing thread, so we can just throw here
                throw exception;
            }
        }
        // success case

        UpdateWorkloadState(workload);
        // TODO: update the emptiness tracking
        // TODO: right now that's not possible, as we can't get the lock on the child qdisc
        // TODO: this is extremely unsafe. FIX THIS!
        __LAST_ENQUEUED_CHILD_INDEX = index;
    }

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (_predicate(state))
        {
            EnqueueDirect(workload);
            return true;
        }
        return false;
    }

    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();

        const int localQueueIndex = 0;
        EarliestDueDateQdisc<THandle>.ChildQdiscState[] childStates = _childStates;

        UpdateWorkloadState(workload);
        // lock to prevent lost updates to the emptiness tracking
        lock (childStates[localQueueIndex].QdiscLock)
        {
            // update the emptiness tracking
            // the actual reset happens in the OnWorkScheduled callback, but we need to
            // set up the index of the child that was just enqueued to for that
            __LAST_ENQUEUED_CHILD_INDEX = localQueueIndex;
            _localQueue.Enqueue(workload);
        }
    }

    protected override void OnWorkScheduled()
    {
        // we are inside a callback of an enqueuing thread
        // load the index of the child that was just enqueued to
        int? lastEnqueuedChildIndex = __LAST_ENQUEUED_CHILD_INDEX;
        if (lastEnqueuedChildIndex is null)
        {
            // this should never happen, as this method can only be part of the enqueueing call stack
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: {nameof(__LAST_ENQUEUED_CHILD_INDEX)} is null.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            // we can actually just throw here, since we aren't in a worker thread
            throw new NotSupportedException("This scheduler does not support scheduling workloads directly onto child qdiscs. Please use the methods provided by the parent workload factory.");
        }
        // clear the empty flag for the child that was just enqueued to
        int index = lastEnqueuedChildIndex.Value;
        ConcurrentBitMap64OLD.UpdateBit(ref _isEmptyMap, index, isSet: false);
        // reset the last enqueued child index
        __LAST_ENQUEUED_CHILD_INDEX = null;
        base.OnWorkScheduled();
    }

    private void UpdateWorkloadState(AbstractWorkloadBase workload)
    {
        EventuallyConsistentVirtualTimeTableEntry timingInformation = _timeTable.GetEntryFor(workload);
        workload._state = new EarliestDueDateState(workload._state)
        {
            VirtualExecutionTime = timingInformation.WorstCaseAverageExecutionTime,
            // we assume worst case execution time for the workload itself
            // but we assume average execution time for the aggregate in TryDequeueInternal
            // this can be tweaked for different scheduling policies
            ExpectedExecutionTime = timingInformation.AverageExecutionTime,
        };
    }

    public override bool RemoveChild(IClasslessQdisc<THandle> child) =>
        RemoveChildCore(child, Timeout.Infinite);

    public override bool TryRemoveChild(IClasslessQdisc<THandle> child) => 
        RemoveChildCore(child, 0);

    private bool RemoveChildCore(IClasslessQdisc<THandle> child, int timeout)
    {
        if (!ContainsChild(child.Handle))
        {
            return false;
        }
        int startTime = Environment.TickCount;
        // wait for child to be empty
        if (!Wait.Until(() => child.IsEmpty, timeout))
        {
            return false;
        }

        // child is empty, attempt to remove it
        using ILockOwnership writeLock = _childModificationLock.AcquireWriteLock();

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
        while (child.TryDequeueInternal(0, false, out AbstractWorkloadBase? workload))
        {
            // enqueue the workloads in the local queue
            _localQueue.Enqueue(workload);
        }

        // remove the child and resize the buffers
        ChildQdiscState[] oldChildStates = _childStates;
        ChildQdiscState[] newChildStates = new ChildQdiscState[oldChildStates.Length - 1];
        int index = -1;
        for (int i = 0; i < oldChildStates.Length && i < newChildStates.Length; i++)
        {
            if (!oldChildStates[i].Child.Qdisc.Handle.Equals(child.Handle))
            {
                newChildStates[i] = oldChildStates[i];
            }
            else
            {
                index = i;
            }
        }
        Debug.Assert(index >= 0);

        // update the emptiness tracking
        ConcurrentBitMap64OLD.RemoveBitAt(ref _isEmptyMap, index);
        _childStates = newChildStates;
        return true;
    }

    public override bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate) => 
        TryAddChildCore(new ChildClassification<THandle>(child, predicate));

    public override bool TryAddChild(IClassfulQdisc<THandle> child) => 
        TryAddChildCore(new ClassfulChildClassification<THandle>(child));

    public override bool TryAddChild(IClasslessQdisc<THandle> child) => 
        TryAddChildCore(new NoChildClassification<THandle>(child));

    private bool TryAddChildCore(IChildClassification<THandle> child)
    {
        using ILockOwnership writeLock = _childModificationLock.AcquireWriteLock();

        ChildQdiscState[] oldChildStates = _childStates;
        if (oldChildStates.Length == 64)
        {
            DebugLog.WriteWarning($"{this}: failed to add child {child.Qdisc} because the maximum number of children has already been reached.", LogWriter.Blocking);
            return false;
        }

        if (TryFindChildUnsafe(child.Qdisc.Handle, out _))
        {
            DebugLog.WriteWarning($"{this}: failed to add child {child.Qdisc} because it is already a child of this qdisc.", LogWriter.Blocking);
            return false;
        }

        // link the child qdisc to the parent qdisc first
        child.Qdisc.InternalInitialize(this);

        ChildQdiscState[] newChildStates = new ChildQdiscState[oldChildStates.Length + 1];
        // copy the old child states and reset all virtual finish times
        for (int i = 0; i < oldChildStates.Length; i++)
        {
            newChildStates[i] = oldChildStates[i];
            // reset the virtual finish time
            // no need to acquire the extra lock.
            // we are the only thread currently allowed to do anything on this qdisc
            Volatile.Write(ref newChildStates[i].LastVirtualFinishTimeRef, 0);
        }
        newChildStates[^1] = new ChildQdiscState(child);
        // update the emptiness tracking
        ConcurrentBitMap64OLD.InsertBitAt(ref _isEmptyMap, newChildStates.Length - 1, false);
        
        // done. write back the new child states
        _childStates = newChildStates;
        return true;
    }

    protected override bool ContainsChild(THandle handle) =>
        TryFindChild(handle, out _);

    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        return TryFindChildUnsafe(handle, out child);
    }

    private bool TryFindChildUnsafe(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child)
    {
        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            child = childStates[i].Child.Qdisc;
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

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;

    private class ChildQdiscState
    {
        private double _lastVirtualFinishTime;
        private AbstractWorkloadBase? _candidate;

        public IChildClassification<THandle> Child { get; }

        public object QdiscLock { get; }

        public ChildQdiscState(IChildClassification<THandle> child)
        {
            Child = child;
            QdiscLock = new object();
            LastVirtualFinishTimeRef = 0;
            CandidateRef = null;
        }

        public ref double LastVirtualFinishTimeRef => ref _lastVirtualFinishTime;

        public ref AbstractWorkloadBase? CandidateRef => ref _candidate;
    }
}