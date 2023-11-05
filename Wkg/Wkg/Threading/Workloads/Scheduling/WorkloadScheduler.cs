using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Scheduling;

using CommonFlags = WorkloadStatus.CommonFlags;

internal class WorkloadScheduler : INotifyWorkScheduled
{
    private readonly IQdisc _rootQdisc;
    private readonly int _maximumConcurrencyLevel;
    private readonly WorkerState _state;

    public WorkloadScheduler(IQdisc rootQdisc, int maximumConcurrencyLevel)
    {
        if (maximumConcurrencyLevel < 1)
        {
            DebugLog.WriteWarning($"The maximum degree of parallelism must be greater than zero. The specified value was {maximumConcurrencyLevel}.", LogWriter.Blocking);
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrencyLevel), maximumConcurrencyLevel, "The maximum degree of parallelism must be greater than zero.");
        }
        _rootQdisc = rootQdisc;
        _maximumConcurrencyLevel = maximumConcurrencyLevel;
        _state = new WorkerState(maximumConcurrencyLevel);

        DebugLog.WriteInfo($"Created workload scheduler with root qdisc {_rootQdisc} and maximum concurrency level {_maximumConcurrencyLevel}.", LogWriter.Blocking);
    }

    public int MaximumConcurrencyLevel => _maximumConcurrencyLevel;

    void INotifyWorkScheduled.OnWorkScheduled()
    {
        DebugLog.WriteDiagnostic("Workload scheduler was poked.", LogWriter.Blocking);

        // this atomic clamped increment is committing, if we have room for another worker, we must start one
        // we are not allowed to abort the operation, because that could lead to starvation
        WorkerStateSnapshot state = _state.ClaimWorkerSlot();
        if (state.CallerClaimedWorkerSlot)
        {
            // we have room for another worker, so we'll start one
            DebugLog.WriteWarning($"Successfully queued new worker {state.CallerWorkerId}. Worker count incremented: {state.WorkerCount - 1} -> {state.WorkerCount}.", LogWriter.Blocking);
            // do not flow the execution context to the worker
            DispatchWorkerNonCapturing(state.CallerWorkerId);
            // we successfully started a worker, so we can exit
            return;
        }
        // we're at the max degree of parallelism, so we can exit
        DebugLog.WriteDiagnostic($"Reached maximum concurrency level: {state.WorkerCount} >= {_maximumConcurrencyLevel}.", LogWriter.Blocking);
    }

    private void DispatchWorkerNonCapturing(int workerId)
    {
        using (ExecutionContext.SuppressFlow())
        {
            ThreadPool.QueueUserWorkItem(WorkerLoop, workerId);
        }
    }

    protected virtual void WorkerLoop(object? state)
    {
        int workerId = (int)state!;
        DebugLog.WriteWarning($"Started worker {workerId}", LogWriter.Blocking);
        bool previousExecutionFailed = false;
        while (TryDequeueOrExitSafely(ref workerId, previousExecutionFailed, out AbstractWorkloadBase? workload))
        {
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.Status.IsOneOf(CommonFlags.Completed));
            workload.InternalRunContinuations();
        }
        DebugLog.WriteWarning($"Terminated worker with previous ID {workerId}.", LogWriter.Blocking);
    }

    /// <summary>
    /// Attempts to dequeue a workload from the root qdisc, and if that fails, attempts to clean up the worker thread and establish a well-defined state with one less worker.
    /// </summary>
    /// <param name="workerId">The id of the worker that is attempting to dequeue a workload.</param>
    /// <param name="previousExecutionFailed"><see langword="true"/> if the previous workload execution failed; <see langword="false"/> if the previous workload execution succeeded. Instructs the underlying qdisc to back track to the previous state if possible.</param>
    /// <param name="workload">The dequeued <see cref="Workload"/>, or <see langword="null"/> if the worker should exit.</param>
    /// <returns><see langword="true"/> if a workload was dequeued, <see langword="false"/> if the worker should exit.</returns>
    /// <remarks>
    /// If this method returns <see langword="false"/>, the worker must exit in order to respect the max degree of parallelism.
    /// </remarks>
    protected bool TryDequeueOrExitSafely(ref int workerId, bool previousExecutionFailed, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        DebugLog.WriteDiagnostic($"Worker {workerId} is attempting to dequeue a workload.", LogWriter.Blocking);
        // race against scheduling threads
        while (true)
        {
            if (_rootQdisc.TryDequeueInternal(workerId, previousExecutionFailed, out workload))
            {
                DebugLog.WriteDiagnostic($"Worker {workerId} successfully dequeued workload {workload}.", LogWriter.Blocking);
                // we successfully dequeued a task, return with success
                return true;
            }
            // we are about to exit, so we must release the worker slot
            // by contract, we can only pass our own worker id to this method
            // once the worker slot is released, we must no longer assume that the worker id is valid
            _state.ResignWorker(workerId);
            // re-sample the queue
            DebugLog.WriteDiagnostic($"Worker holding ID {workerId} previously found no tasks, resampling root qdisc to ensure true emptiness.", LogWriter.Blocking);
            // it is the responsibility of the qdisc implementation to ensure that this operation is thread-safe
            if (_rootQdisc.IsEmpty)
            {
                // no more tasks, exit
                DebugLog.WriteWarning($"Worker holding ID {workerId} previously found no tasks, exiting.", LogWriter.Blocking);
                return false;
            }
            DebugLog.WriteDiagnostic($"Worker holding ID {workerId} previously was interrupted while exiting due to new scheduling activity. Attempting to restore worker count.", LogWriter.Blocking);
            // failure case: someone else scheduled a task, possibly before we decremented the worker count,
            // so we could lose a worker here attempt abort the exit by re-claiming the worker slot
            // we could be racing against a scheduling thread, or any other worker that is also trying to exit
            // we attempt to atomically restore the worker count to the previous value and claim a new worker slot
            // note that restoring the worker count may result in a different worker id being assigned to us
            WorkerStateSnapshot state = _state.ClaimWorkerSlot();
            if (!state.CallerClaimedWorkerSlot)
            {
                // somehow someone else comitted to creating a new worker, that's unfortunate due to the scheduling overhead
                // but they are committed now, so we must give up and exit
                DebugLog.WriteWarning($"Worker holding ID {workerId} previously is exiting after encountering maximum concurrency level {_maximumConcurrencyLevel} during restore attempt.", LogWriter.Blocking);
                return false;
            }
            DebugLog.WriteWarning($"Worker holding ID {workerId} previously is resuming with new ID {state.CallerWorkerId} after successfully restoring worker count.", LogWriter.Blocking);
            workerId = state.CallerWorkerId;
        }
    }

    private protected class WorkerState
    {
        private readonly int[] _workerIds;
        private readonly int[] _writeRequestPriorities;
        private int _currentDegreeOfParallelism;

        public WorkerState(int maximumConcurrencyLevel)
        {
            _writeRequestPriorities = new int[maximumConcurrencyLevel];
            _workerIds = new int[maximumConcurrencyLevel];
            for (int i = 0; i < _workerIds.Length; i++)
            {
                _workerIds[i] = i;
            }
            _currentDegreeOfParallelism = 0;
        }

        public WorkerStateSnapshot ClaimWorkerSlot()
        {
            DebugLog.WriteDiagnostic("Attempting to claim worker slot.", LogWriter.Blocking);
            // post increment, so we start at 0
            int index = Atomic.IncrementClampMaxFast(ref _currentDegreeOfParallelism, _workerIds.Length);
            int workerId = -1;
            if (index < _workerIds.Length)
            {
                DebugLog.WriteWarning($"Attempting to claim worker slot {index}.", LogWriter.Blocking);
                SpinWait spinner = default;
                workerId = Interlocked.Exchange(ref _workerIds[index], -1);
                for (int i = 0; workerId == -1; i++)
                {
                    DebugLog.WriteFatal($"Worker slot {index} is not yet available, spinning {i}.", LogWriter.Blocking);
                    spinner.SpinOnce();
                    workerId = Interlocked.Exchange(ref _workerIds[index], -1);
                }
            }
            return new WorkerStateSnapshot(workerId, index + 1);
        }

        public void ResignWorker(int workerId)
        {
            SpinWait spinner = default;
            // pre-decrement, so we start at the maximum value - 1
            int index = Interlocked.Decrement(ref _currentDegreeOfParallelism);
            DebugLog.WriteWarning($"Resigning worker {workerId} to slot {index}.", LogWriter.Blocking);
            int myPriority = 0;
            int currentPriority = Volatile.Read(ref _writeRequestPriorities[index]);
            while (currentPriority > myPriority)
            {
                DebugLog.WriteError($"Worker {workerId} is waiting for slot {index} to be released for write request priority {currentPriority} to be lower than or equal to {myPriority}.", LogWriter.Blocking);
                spinner.SpinOnce();
                myPriority++;
                currentPriority = Volatile.Read(ref _writeRequestPriorities[index]);
            }
            int oldValue = -1;
            if (currentPriority == 0)
            {
                oldValue = Interlocked.CompareExchange(ref _workerIds[index], workerId, -1);
            }
            for (; oldValue != -1; myPriority++)
            {
                DebugLog.WriteFatal($"Worker slot {index} is not yet empty (read {oldValue}), spinning {myPriority}.", LogWriter.Blocking);
                if (Atomic.WriteMaxFast(ref _writeRequestPriorities[index], myPriority) <= myPriority)
                {
                    DebugLog.WriteError($"Worker {workerId} is attempting to claim slot {index} for write request priority {myPriority}.", LogWriter.Blocking);
                    oldValue = Interlocked.CompareExchange(ref _workerIds[index], workerId, -1);
                    if (oldValue != -1)
                    {
                        Interlocked.Exchange(ref _writeRequestPriorities[index], 0);
                        break;
                    }
                }
                else
                {
                    DebugLog.WriteError($"Worker {workerId} is waiting for slot {index} to be released for write request priority {myPriority} to be lower than or equal to {myPriority}.", LogWriter.Blocking);
                }
                spinner.SpinOnce();
            }
            DebugLog.WriteError($"Worker {workerId} successfully resigned to slot {index}.", LogWriter.Blocking);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
    private protected readonly ref struct WorkerStateSnapshot
    {
        [FieldOffset(0)]
        public readonly int CallerWorkerId;
        [FieldOffset(4)]
        public readonly int WorkerCount;

        public WorkerStateSnapshot(int callerWorkerId, int workerCount)
        {
            CallerWorkerId = callerWorkerId;
            WorkerCount = workerCount;
        }

        public bool CallerClaimedWorkerSlot => CallerWorkerId != -1;
    }
}
