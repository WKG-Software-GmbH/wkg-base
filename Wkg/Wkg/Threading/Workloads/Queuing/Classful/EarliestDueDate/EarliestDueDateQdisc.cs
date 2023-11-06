using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classful.Classification.Internals;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.VirtualTime;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

internal class EarliestDueDateQdisc<THandle> : ClassfulQdisc<THandle> where THandle : unmanaged
{
    private readonly IVirtualTimeTable _timeTable;
    private readonly ReaderWriterLockSlim _childModificationLock = new();
    private readonly IClasslessQdisc<THandle> _localQueue;
    private readonly Predicate<object?> _predicate;

    // TODO: these can probably just be volatile
    private IChildClassification<THandle>[] _children;
    private AbstractWorkloadBase[] _candicateBuffer;
    private double[] _lastVirtualFinishTimes;

    public EarliestDueDateQdisc(THandle handle, Predicate<object?> predicate, IClasslessQdiscBuilder inner, int concurrencyLevel) : base(handle)
    {
        _timeTable = VirtualTimeTable.CreateFast(concurrencyLevel, 32);
        _predicate = predicate;
        _localQueue = inner.BuildUnsafe(default(THandle));
        _children = new IChildClassification<THandle>[1] { new NoChildClassification<THandle>(_localQueue) };
        _candicateBuffer = new AbstractWorkloadBase[1];
        _lastVirtualFinishTimes = new double[1];
        _predicate = predicate;
    }

    public override bool IsEmpty => throw new NotImplementedException();

    public override int Count => throw new NotImplementedException();

    protected override bool CanClassify(object? state)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        try
        {
            _childModificationLock.EnterReadLock();

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].CanClassify(state))
                {
                    return true;
                }
            }
        }
        finally
        {
            _childModificationLock.ExitReadLock();
        }
        return false;
    }

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload)
    {
        // recursive classification of child qdiscs only.
        // matching our own predicate is the job of the parent qdisc.
        try
        {
            _childModificationLock.EnterReadLock();

            IChildClassification<THandle>[] children = Volatile.Read(ref _children);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].CanClassify(state))
                {
                    lock (children[i])
                    {
                        UpdateTimeUnsafe(workload, i);
                    }
                    if (!children[i].TryEnqueue(state, workload))
                    {
                        // this should never happen, as we already checked if the child can classify the workload
                        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Scheduler inconsistency: child qdisc {children[i].Qdisc} reported to be able to classify workload {workload}, but failed to do so.");
                        Debug.Fail(exception.Message);
                        DebugLog.WriteException(exception, LogWriter.Blocking);
                        workload.InternalAbort(exception);
                        // this is not a fatal error, but we now have a phantom workload in virtual time tracking
                    }
                    return true;
                }
            }
        }
        finally
        {
            _childModificationLock.ExitReadLock();
        }
        return false;
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
        try
        {
            _childModificationLock.EnterReadLock();

            const int localQueueIndex = 0;
            lock (_children[localQueueIndex])
            {
                UpdateTimeUnsafe(workload, localQueueIndex);
            }
            _localQueue.Enqueue(workload);
        }
        finally
        {
            _childModificationLock.ExitReadLock();
        }
    }

    private void UpdateTimeUnsafe(AbstractWorkloadBase workload, int childIndex)
    {
        EventuallyConsistentVirtualTimeTableEntry timingInformation = _timeTable.GetEntryFor(workload);
        double virtualStartTime = Math.Max(Volatile.Read(ref _lastVirtualFinishTimes[childIndex]), _timeTable.Now());
        double virtualFinishTime = virtualStartTime + timingInformation.WorstCaseAverageExecutionTime;
        Volatile.Write(ref _lastVirtualFinishTimes[childIndex], virtualFinishTime);
        workload._state = new EarliestDueDateState(workload._state)
        {
            VirtualFinishTime = virtualFinishTime
        };
    }

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => throw new NotImplementedException();

    protected override void OnWorkScheduled()
    {
        // TODO: reset emptiness state tracking
        base.OnWorkScheduled();
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
        try
        {
            int startTime = Environment.TickCount;
            // wait for child to be empty
            if (!Wait.Until(() => child.IsEmpty, timeout))
            {
                return false;
            }

            // child is empty, attempt to remove it
            _childModificationLock.EnterWriteLock();

            // check if child is still there
            if (!ContainsChild(child.Handle))
            {
                return false;
            }

            // mark the child as completed (all new scheduling attempts will fail)
            child.Complete();

            // someone may have scheduled new workloads in the meantime
            // we preserve them by moving them to the local queue
            // this may break the intended scheduling order, but it is better than losing workloads
            // also that is acceptable, as it should happen very rarely and only if the user is doing something wrong
            while (child.TryDequeueInternal(0, false, out AbstractWorkloadBase? workload))
            {
                // enqueue the workloads in the local queue
                _localQueue.Enqueue(workload);
            }

            // remove the child and resize the buffers
            IChildClassification<THandle>[] oldChildren = Volatile.Read(ref _children);
            AbstractWorkloadBase[] oldBuffer = Volatile.Read(ref _candicateBuffer);
            double[] lastVirtualFinishTimes = Volatile.Read(ref _lastVirtualFinishTimes);

            IChildClassification<THandle>[] newChildren = new IChildClassification<THandle>[oldChildren.Length - 1];
            AbstractWorkloadBase[] newBuffer = new AbstractWorkloadBase[newChildren.Length];
            double[] newLastVirtualFinishTimes = new double[newChildren.Length];
            for (int i = 0; i < oldChildren.Length && i < newBuffer.Length; i++)
            {
                if (!oldChildren[i].Qdisc.Handle.Equals(child.Handle))
                {
                    newChildren[i] = oldChildren[i];
                    newBuffer[i] = oldBuffer[i];
                    newLastVirtualFinishTimes[i] = lastVirtualFinishTimes[i];
                }
            }

            Volatile.Write(ref _children, newChildren);
            Volatile.Write(ref _candicateBuffer, newBuffer);
            Volatile.Write(ref _lastVirtualFinishTimes, newLastVirtualFinishTimes);
        }
        finally
        {
            _childModificationLock.ExitWriteLock();
        }
        return true;
    }

    public override bool TryAddChild(IClasslessQdisc<THandle> child, Predicate<object?> predicate) => throw new NotImplementedException();
    public override bool TryAddChild(IClassfulQdisc<THandle> child) => throw new NotImplementedException();
    public override bool TryAddChild(IClasslessQdisc<THandle> child) => throw new NotImplementedException();
    protected override bool ContainsChild(THandle handle) => throw new NotImplementedException();
    protected override bool TryFindChild(THandle handle, [NotNullWhen(true)] out IClasslessQdisc<THandle>? child) => throw new NotImplementedException();
    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => throw new NotImplementedException();
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;
}
