using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Collections.Concurrent;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Extensions;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Routing;
using Wkg.Threading.Workloads.Queuing.VirtualTime;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

internal class GfqQdisc<THandle> : ClassfulQdisc<THandle> where THandle : unmanaged
{
    [ThreadStatic]
    private static int? __LAST_ENQUEUED_CHILD_INDEX;

    private readonly IVirtualTimeTable _timeTable;
    private readonly ReaderWriterLockSlim _childModificationLock = new();
    private readonly ReaderWriterLockSlim _schedulerLock = new();
    private readonly IClassifyingQdisc<THandle> _localQueue;
    private readonly VirtualFinishTimeFunction _virtualFinishTimeFunction;
    private readonly VirtualExecutionTimeFunction _virtualExecutionTimeFunction;
    private readonly VirtualAccumulatedFinishTimeFunction _virtualAccumulatedFinishTimeFunction;

    private uint _generationCounter;
    private readonly ConcurrentBitmap _hasDataMap;
    private volatile ChildQdiscState[] _childStates;
    private int _maxRoutingPathDepthEncountered = 2;

    public GfqQdisc(THandle handle, GfqQdiscParams parameters) : base(handle, parameters.Predicate)
    {
        ArgumentNullException.ThrowIfNull(parameters.Predicate, nameof(parameters.Predicate));
        ArgumentNullException.ThrowIfNull(parameters.Inner, nameof(parameters.Inner));
        ArgumentNullException.ThrowIfNull(parameters.VirtualFinishTimeFunction, nameof(parameters.VirtualFinishTimeFunction));
        ArgumentNullException.ThrowIfNull(parameters.VirtualExecutionTimeFunction, nameof(parameters.VirtualExecutionTimeFunction));
        ArgumentNullException.ThrowIfNull(parameters.VirtualAccumulatedFinishTimeFunction, nameof(parameters.VirtualAccumulatedFinishTimeFunction));

        _timeTable = parameters.PreferPreciseMeasurements
            ? VirtualTimeTable.CreatePrecise(parameters.ConcurrencyLevel, parameters.ExpectedNumberOfDistinctPayloads, parameters.MeasurementSampleLimit)
            : VirtualTimeTable.CreateFast(parameters.ConcurrencyLevel, parameters.ExpectedNumberOfDistinctPayloads, parameters.MeasurementSampleLimit);
        _virtualFinishTimeFunction = parameters.VirtualFinishTimeFunction;
        _virtualExecutionTimeFunction = parameters.VirtualExecutionTimeFunction;
        _virtualAccumulatedFinishTimeFunction = parameters.VirtualAccumulatedFinishTimeFunction;
        _localQueue = parameters.Inner.BuildUnsafe(default(THandle), MatchNothingPredicate);
        _childStates = [new ChildQdiscState(_localQueue, new GfqWeight(1d, 1d))];
        // by default we have one child, so the bit map is initialized with a single bit
        // if we have more children, the bit map will be resized automatically
        _hasDataMap = new ConcurrentBitmap(1);
        _hasDataMap.UpdateBit(0, isSet: true);
    }

    /// <inheritdoc/>
    protected override void OnInternalInitialize(INotifyWorkScheduled parentScheduler) =>
        BindChildQdisc(_localQueue);

    public override int BestEffortCount
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
                count += childStates[i].Child.BestEffortCount;
            }
            // don't forget the local caches
            // just pop-count all the bits that are not set
            return count + _hasDataMap.UnsafePopCount;
        }
    }

    public override bool IsEmpty => IsKnownEmptyVolatileUnsafe;

    private bool IsKnownEmptyVolatileUnsafe => _hasDataMap.IsEmpty;

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
                        GfqState state = (GfqState)workload._state!;
                        EventuallyConsistentVirtualTimeTableEntry latestTimingInfo = _timeTable.GetEntryFor(workload);
                        // we assume average execution time for the aggregate
                        // but we assume worst case execution time for the workload itself
                        // this can be tweaked for different scheduling policies
                        // we are the only ones able to update the virtual finish time (we already hold the lock on the child qdisc)
                        double lastVirtualFinishTime = Volatile.Read(ref child.LastVirtualFinishTimeRef);
                        double newVirtualFinishTime = _virtualAccumulatedFinishTimeFunction.Invoke(state.QdiscWeight, _timeTable, state.TimingInfo, lastVirtualFinishTime);
                        DebugLog.WriteDiagnostic($"{this}: dequeued workload {workload} from child {child.Child}. Virtual finish time increased by {newVirtualFinishTime - lastVirtualFinishTime} to {newVirtualFinishTime}.", LogWriter.Blocking);
                        Volatile.Write(ref child.LastVirtualFinishTimeRef, newVirtualFinishTime);
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
            if (!_hasDataMap.IsBitSet(i))
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
                DebugLog.WriteDiagnostic($"{this}: candidate buffer for child {childState.Child} is empty, but bit map indicates that it is not empty. Attempting to repopulate the candidate buffer.", LogWriter.Blocking);
                // there are two reasons why the candidate buffer can be empty:
                // 1. a workload was just enqueued to the child and we are the first worker to notice that the cache wasn't loaded yet
                // 2. the emptiness state changed since the last time we checked
                // in any case, we need to acquire the child qdisc lock to determine what's going on here
                // we only do a try enter though, as, if another worker is currently repopulating the candidate buffer, we can check back on it later.
                // we only want to ensure that the candidate buffer is repopulated at all, not that we are the ones doing it.
                if (Monitor.TryEnter(childState.QdiscLock))
                {
                    // re-sample the candidate buffer as well, as it may have changed between our last check and us acquiring the lock
                    possibleCandidate = Volatile.Read(ref childState.CandidateRef);
                    if (possibleCandidate is not null)
                    {
                        // the candidate buffer was repopulated before we got to the TryEnter call
                        // this is a totally fine but rare scenario, as we only need to ensure that the candidate buffer is repopulated
                        // we can just continue as normal
                        Debug.Assert(_hasDataMap.IsBitSet(i));
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: candidate buffer for child {childState.Child} was repopulated before we got to the TryEnter call. Continuing as normal.", LogWriter.Blocking);
                    }
                    // try to populate if: the data bit is set and the candidate buffer is still empty
                    else if (_hasDataMap.IsBitSet(i) && TryRepopulateCandidateUnsafe(childState, workerId, i, out possibleCandidate))
                    {
                        // the child is not empty anymore, we successfully repopulated the candidate buffer
                        // continue as normal
                        originalGenerationCounter = Interlocked.Increment(ref _generationCounter);
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: successfully repopulated candidate buffer for child {childState.Child}. Generation counter is now {originalGenerationCounter}.", LogWriter.Blocking);
                    }
                    else
                    {
                        // the child became empty while since our last check
                        // or we ourselves determined that the child is empty
                        // in any case, we can skip this child
                        Monitor.Exit(childState.QdiscLock);
                        DebugLog.WriteDiagnostic($"{this}: child {childState.Child} is empty. Skipping.", LogWriter.Blocking);
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
                    DebugLog.WriteDiagnostic($"{this}: failed to acquire child qdisc lock for child {childState.Child}. Skipping for now.", LogWriter.Blocking);
                    continue;
                }
            }
            // we actually found a non-null candidate.
            if (possibleCandidate._state is not GfqState candidateState)
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
                WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: workload {possibleCandidate} has no {nameof(GfqState)}.");
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
            double lastVirtualFinishTime = Volatile.Read(ref childState.LastVirtualFinishTimeRef);
            double virtualExecutionTime = _virtualExecutionTimeFunction.Invoke(candidateState.QdiscWeight, _timeTable, candidateState.TimingInfo);
            double virtualFinishTime = _virtualFinishTimeFunction.Invoke(candidateState.QdiscWeight, _timeTable, virtualExecutionTime, lastVirtualFinishTime);
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
        DebugLog.WriteDiagnostic($"{this}: attempting to repopulate candidate buffer for child {child.Child}.", LogWriter.Blocking);
        byte token;
        int i = 0;
        do
        {
            if (i != 0)
            {
                DebugLog.WriteDebug($"{this}: emptiness bit map changed while attempting to declare child {child.Child} as empty. Attempts so far {i}. Resampling...", LogWriter.Blocking);
            }
            token = _hasDataMap.GetToken(childIndex);
            if (child.Child.TryDequeueInternal(workerId, false, out workload))
            {
                // we successfully dequeued a workload from the child
                Volatile.Write(ref child.CandidateRef, workload);
                DebugLog.WriteDiagnostic($"{this}: successfully repopulated candidate buffer for child {child.Child}.", LogWriter.Blocking);
                return true;
            }
            // the child is empty
            // mark it as empty
            DebugLog.WriteDiagnostic($"{this}: child {child.Child} seems to be empty. Updating emptiness bit map.", LogWriter.Blocking);
            i++;
        } while (!_hasDataMap.TryUpdateBit(childIndex, token, isSet: false));
        DebugLog.WriteDebug($"{this}: failed to repopulate candidate buffer for child {child.Child}. Marked child as empty.", LogWriter.Blocking);
        return false;
    }

    protected override bool CanClassify(object? state)
    {
        if (Predicate.Invoke(state))
        {
            // fast path. we can classify the workload ourselves
            return true;
        }

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

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        // multiple threads are allowed to enqueue, just counting workloads (Count property) must be exclusive
        using ILockOwnership enqueueLock = _schedulerLock.AcquireReadLock();

        ChildQdiscState[] childStates = _childStates;
        RoutingPath<THandle> path = new(Volatile.Read(ref _maxRoutingPathDepthEncountered));
        for (int i = 0; i < childStates.Length; i++)
        {
            ChildQdiscState childState = childStates[i];
            if (childState.Child.Handle.Equals(handle))
            {
                UpdateWorkloadState(workload, childState.Weight);
                // update the emptiness tracking
                // the actual reset happens in the OnWorkScheduled callback, but we need to
                // set up the index of the child that was just enqueued to for that
                __LAST_ENQUEUED_CHILD_INDEX = i;
                childState.Child.Enqueue(workload);
                DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {childState.Child}.", LogWriter.Blocking);
                goto SUCCESS;
            }
            // we must first check if the child can enqueue the workload.
            // then we must prepare everything for the enqueueing operation.
            // only then can we actually enqueue the workload.
            // in order to achieve this we construct a routing path and then directly enqueue the workload to the child.
            // using a routing path allows us to avoid having to do the same work twice.
            if (childState.Child.TryFindRoute(handle, ref path))
            {
                // ensure that the path is complete and valid
                WorkloadSchedulingException.ThrowIfRoutingPathLeafIsInvalid(path.Leaf, handle);

                // this child can enqueue the workload
                UpdateWorkloadState(workload, childState.Weight);
                // update the emptiness tracking
                // the actual reset happens in the OnWorkScheduled callback, but we need to
                // set up the index of the child that was just enqueued to for that
                __LAST_ENQUEUED_CHILD_INDEX = i;

                // we need to call WillEnqueueFromRoutingPath on all nodes in the path
                // failure to do so may result in incorrect emptiness tracking of the child qdiscs
                foreach (ref readonly RoutingPathNode<THandle> node in path)
                {
                    node.Qdisc.WillEnqueueFromRoutingPath(in node, workload);
                }
                // enqueue the workload to the leaf
                path.Leaf.Enqueue(workload);
                Atomic.WriteMaxFast(ref _maxRoutingPathDepthEncountered, path.Count);
                DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {childState.Child}.", LogWriter.Blocking);
                goto SUCCESS;
            }
        }
        path.Dispose();
        return false;
    SUCCESS:
        path.Dispose();
        return true;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        using (ILockOwnership readLock = _childModificationLock.AcquireReadLock())
        {
            using ILockOwnership enqueueLock = _schedulerLock.AcquireReadLock();
            ChildQdiscState[] childStates = _childStates;
            for (int i = 0; i < childStates.Length; i++)
            {
                ChildQdiscState childState = childStates[i];
                if (childState.Child.CanClassify(state))
                {
                    UpdateWorkloadState(workload, childState.Weight);
                    // update the emptiness tracking
                    // the actual reset happens in the OnWorkScheduled callback, but we need to
                    // set up the index of the child that was just enqueued to for that
                    __LAST_ENQUEUED_CHILD_INDEX = i;
                    if (!childState.Child.TryEnqueue(state, workload))
                    {
                        // this should never happen, as we already checked if the child can classify the workload
                        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {childState.Child} reported to be able to classify workload {workload}, but failed to do so.");
                        Debug.Fail(exception.Message);
                        DebugLog.WriteException(exception, LogWriter.Blocking);
                        // we are on the enqueueing thread, so we can just throw here
                        throw exception;
                    }
                    DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to child {childState.Child}.", LogWriter.Blocking);
                    return true;
                }
            }
        }

        return TryEnqueueDirect(state, workload);
    }

    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();

        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            IClassifyingQdisc<THandle> child = childStates[i].Child;
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
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        ChildQdiscState[] childStates = _childStates;

        int index = routingPathNode.Offset;
        ChildQdiscState? childState = null;

        if (index < childStates.Length && childStates[index].Child.Handle.Equals(routingPathNode.Handle))
        {
            // fast path. the cached offset is still valid
            childState = childStates[index];
        }
        else
        {
            // slow path. the cached offset is no longer valid
            for (int i = 0; i < childStates.Length; i++)
            {
                if (childStates[i].Child.Handle.Equals(routingPathNode.Handle))
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
        DebugLog.WriteDiagnostic($"{this}: expecting to enqueue workload {workload} to child {childState.Child} via routing path.", LogWriter.Blocking);

        UpdateWorkloadState(workload, childState.Weight);
        __LAST_ENQUEUED_CHILD_INDEX = index;
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
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        using ILockOwnership enqueueLock = _schedulerLock.AcquireReadLock();

        const int localQueueIndex = 0;
        ChildQdiscState[] childStates = _childStates;

        UpdateWorkloadState(workload, childStates[0].Weight);
        // update the emptiness tracking
        // the actual reset happens in the OnWorkScheduled callback, but we need to
        // set up the index of the child that was just enqueued to for that
        __LAST_ENQUEUED_CHILD_INDEX = localQueueIndex;
        _localQueue.Enqueue(workload);
        DebugLog.WriteDiagnostic($"{this}: enqueued workload {workload} to local queue.", LogWriter.Blocking);
    }

    protected override void OnWorkScheduled()
    {
        if (_childModificationLock.IsWriteLockHeld)
        {
            // we are inside a callback from some child modification method
            // we don't need to do anything here, as the child modification method will take care of the emptiness tracking
            // we must also break the notification chain here, as no "real" enqueueing operation happened
            return;
        }
        // we are inside a callback of an enqueuing thread
        // load the index of the child that was just enqueued to
        int? lastEnqueuedChildIndex = __LAST_ENQUEUED_CHILD_INDEX;
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
        // no token is required, as we just force the bit to be set
        // worker threads attempting to mark this child as empty will just fail to do so as their token will be invalidated by us
        // so no ABA problem here (not empty -> worker finds no workload -> we set it to not empty -> worker tries to set it to empty -> worker fails)
        _hasDataMap.UpdateBit(index, isSet: true);
        // reset the last enqueued child index
        __LAST_ENQUEUED_CHILD_INDEX = null;
        DebugLog.WriteDebug($"{this}: cleared empty flag for child {index}.", LogWriter.Blocking);
        base.OnWorkScheduled();
    }

    private void UpdateWorkloadState(AbstractWorkloadBase workload, GfqWeight weight)
    {
        EventuallyConsistentVirtualTimeTableEntry timingInformation = _timeTable.GetEntryFor(workload);
        workload._state = new GfqState(workload._state, timingInformation, weight);
    }

    public override bool RemoveChild(IClassifyingQdisc<THandle> child) =>
        RemoveChildCore(child, Timeout.Infinite);

    public override bool TryRemoveChild(IClassifyingQdisc<THandle> child) =>
        RemoveChildCore(child, 0);

    private bool RemoveChildCore(IClassifyingQdisc<THandle> child, int timeout)
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
            _hasDataMap.UpdateBit(localQueueIndex, isSet: true);
        }

        // remove the child and resize the buffers
        ChildQdiscState[] oldChildStates = _childStates;
        ChildQdiscState[] newChildStates = new ChildQdiscState[oldChildStates.Length - 1];
        int index = -1;
        for (int i = 0; i < oldChildStates.Length && i < newChildStates.Length; i++)
        {
            if (!oldChildStates[i].Child.Handle.Equals(child.Handle))
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
        _hasDataMap.RemoveBitAt(index, shrink: true);
        _childStates = newChildStates;
        return true;
    }

    public bool TryAddChild(IClassifyingQdisc<THandle> child, GfqWeight weight) =>
        TryAddChildCore(child, weight);

    public override bool TryAddChild(IClassifyingQdisc<THandle> child) =>
        TryAddChildCore(child, new GfqWeight(1d, 1d));

    private bool TryAddChildCore(IClassifyingQdisc<THandle> child, GfqWeight weight)
    {
        using ILockOwnership writeLock = _childModificationLock.AcquireWriteLock();

        ChildQdiscState[] oldChildStates = _childStates;

        if (TryFindChildUnsafe(child.Handle, out _))
        {
            DebugLog.WriteWarning($"{this}: failed to add child {child} because it is already a child of this qdisc.", LogWriter.Blocking);
            return false;
        }

        // link the child qdisc to the parent qdisc first
        child.InternalInitialize(this);

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
        newChildStates[^1] = new ChildQdiscState(child, weight);
        // update the emptiness tracking
        // instead of inserting a new bit, we just grow and update the last bit
        // this is a cheaper operation as we don't need to hold a write lock for the update
        // and growing the bit map by one bit is a cheap operation if we don't hit any segment or cluster boundaries
        Debug.Assert(_hasDataMap.Length == newChildStates.Length - 1);
        _hasDataMap.Grow(additionalSize: 1);
        _hasDataMap.UpdateBit(newChildStates.Length - 1, isSet: false);

        // done. write back the new child states
        _childStates = newChildStates;
        return true;
    }

    protected override bool ContainsChild(THandle handle) =>
        TryFindChild(handle, out _);

    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child)
    {
        using ILockOwnership readLock = _childModificationLock.AcquireReadLock();
        return TryFindChildUnsafe(handle, out child);
    }

    private bool TryFindChildUnsafe(THandle handle, [NotNullWhen(true)] out IClassifyingQdisc<THandle>? child)
    {
        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            child = childStates[i].Child;
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

    protected override void OnWorkerTerminated(int workerId)
    {
        // forward to children, no lock needed. if children are removed then they don't need to be notified
        // and if new children are added, they shouldn't know about the worker anyway
        ChildQdiscState[] childStates = _childStates;
        for (int i = 0; i < childStates.Length; i++)
        {
            childStates[i].Child.OnWorkerTerminated(workerId);
        }

        base.OnWorkerTerminated(workerId);
    }

    protected override void DisposeManaged()
    {
        // by contract, we should be the only thread accessing the qdisc at this point
        // so we don't need to acquire any locks
        _schedulerLock.Dispose();
        _childModificationLock.Dispose();
        _hasDataMap.Dispose();
        ChildQdiscState[] childStates = Interlocked.Exchange(ref _childStates, []);
        foreach (ChildQdiscState childState in childStates)
        {
            childState.Child.Complete();
            childState.Child.Dispose();
        }
    }

    [DebuggerDisplay("Qdisc: {Child}, Count: {Child.Count} LVFT: {_lastVirtualFinishTime}, Candidate: {_candidate}")]
    private class ChildQdiscState
    {
        private double _lastVirtualFinishTime;
        private AbstractWorkloadBase? _candidate;

        public IClassifyingQdisc<THandle> Child { get; }

        public GfqWeight Weight { get; }

        public object QdiscLock { get; }

        public ChildQdiscState(IClassifyingQdisc<THandle> child, GfqWeight weight)
        {
            Child = child;
            QdiscLock = new object();
            LastVirtualFinishTimeRef = 0;
            CandidateRef = null;
            Weight = weight;
        }

        public ref double LastVirtualFinishTimeRef => ref _lastVirtualFinishTime;

        public ref AbstractWorkloadBase? CandidateRef => ref _candidate;
    }
}