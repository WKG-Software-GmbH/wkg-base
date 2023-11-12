using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
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
    protected readonly ManualResetEventSlim _fullyDisposed = new(false);
    protected volatile bool _disposed;

    // do not mark as readonly, this struct is mutable
    // tracks the current degree of parallelism and the worker ids that are currently in use
    private WorkerState _state;

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
        if (!_disposed && state.CallerClaimedWorkerSlot)
        {
            // we have room for another worker, so we'll start one
            DebugLog.WriteDiagnostic($"Successfully queued new worker {state.CallerWorkerId}. Worker count incremented: {state.WorkerCount - 1} -> {state.WorkerCount}.", LogWriter.Blocking);
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
        DebugLog.WriteInfo($"Started worker {workerId}", LogWriter.Blocking);
        bool previousExecutionFailed = false;
        // check for disposal before and after each dequeue (volatile read)
        AbstractWorkloadBase? workload = null;
        int previousWorkerId = workerId;
        while (!_disposed && TryDequeueOrExitSafely(ref workerId, previousExecutionFailed, out workload) && !_disposed)
        {
            previousWorkerId = workerId;
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.Status.IsOneOf(CommonFlags.Completed));
            workload.InternalRunContinuations(workerId);
        }
        OnWorkerTerminated(ref workerId, previousWorkerId, workload);
    }

    protected void OnWorkerTerminated(ref int workerId, int previousWorkerId, AbstractWorkloadBase? workload)
    {
        if (workerId != -1)
        {
            previousWorkerId = workerId;
            _state.ResignWorker(ref workerId);
        }
        Debug.Assert(workerId == -1);
        if (_disposed)
        {
            if (workload?.IsCompleted is false)
            {
                // if we dequeued a workload, but the scheduler was disposed, we need to abort the workload
                workload.InternalAbort(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(WorkloadSchedulerWithDI))));
                DebugLog.WriteDiagnostic($"Aborted workload {workload} due to scheduler disposal.", LogWriter.Blocking);
            }
            if (_state.VolatileWorkerCount <= 0)
            {
                _fullyDisposed.Set();
            }
            DebugLog.WriteInfo($"Scheduler was disposed. Terminated worker {previousWorkerId}.", LogWriter.Blocking);
        }
        else
        {
            DebugLog.WriteInfo($"Terminated worker {previousWorkerId}.", LogWriter.Blocking);
        }
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
        // race against scheduling threads (unless we are being disposed)
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
            // before we release the worker slot, we must allow the qdiscs to clean up worker-local states
            _rootQdisc.OnWorkerTerminated(workerId);
            // now we can release the worker slot
            int previousWorkerId = workerId;
            _state.ResignWorker(ref workerId);
            // re-sample the queue
            DebugLog.WriteDiagnostic($"Worker holding ID {previousWorkerId} previously found no tasks, resampling root qdisc to ensure true emptiness.", LogWriter.Blocking);
            // it is the responsibility of the qdisc implementation to ensure that this operation is thread-safe
            if (_disposed || _rootQdisc.IsEmpty)
            {
                if (_disposed)
                {
                    // we are being disposed, exit
                    DebugLog.WriteDebug($"Worker holding ID {previousWorkerId} previously found no tasks, exiting due to disposal.", LogWriter.Blocking);
                }
                else
                {
                    // no more tasks, exit
                    DebugLog.WriteDebug($"Worker holding ID {previousWorkerId} previously found no tasks, exiting.", LogWriter.Blocking);
                }
                return false;
            }
            DebugLog.WriteDiagnostic($"Worker holding ID {previousWorkerId} previously was interrupted while exiting due to new scheduling activity. Attempting to restore worker count.", LogWriter.Blocking);
            // failure case: someone else scheduled a task, possibly before we decremented the worker count,
            // so we could lose a worker here attempt abort the exit by re-claiming the worker slot
            // we could be racing against a scheduling thread, or any other worker that is also trying to exit
            // we attempt to atomically restore the worker count to the previous value and claim a new worker slot
            // note that restoring the worker count may result in a different worker id being assigned to us
            WorkerStateSnapshot state = _state.ClaimWorkerSlot();
            if (_disposed || !state.CallerClaimedWorkerSlot)
            {
                if (_disposed)
                {
                    // we are being disposed, exit
                    DebugLog.WriteDebug($"Worker holding ID {previousWorkerId} previously found no tasks, exiting due to disposal.", LogWriter.Blocking);
                }
                else
                {
                    // somehow someone else comitted to creating a new worker, that's unfortunate due to the scheduling overhead
                    // but they are committed now, so we must give up and exit
                    DebugLog.WriteDebug($"Worker holding ID {previousWorkerId} previously is exiting after encountering maximum concurrency level {_maximumConcurrencyLevel} during restore attempt.", LogWriter.Blocking);
                }
                return false;
            }
            workerId = state.CallerWorkerId;
            DebugLog.WriteDebug($"Worker holding ID {previousWorkerId} previously is resuming with new ID {workerId} after successfully restoring worker count.", LogWriter.Blocking);
        }
    }

    void INotifyWorkScheduled.DisposeRoot()
    {
        DebugLog.WriteInfo($"Disposing workload scheduler with root qdisc {_rootQdisc}...", LogWriter.Blocking);
        _disposed = true;
        // it would be increadibly bad if we started waiting before we set the disposed flag
        Thread.MemoryBarrier();
        DebugLog.WriteInfo($"Waiting for all workers to terminate...", LogWriter.Blocking);
        if (_state.VolatileWorkerCount > 0)
        {
            _fullyDisposed.Wait();
        }
        DebugLog.WriteInfo($"All workers terminated.", LogWriter.Blocking);
    }

    private protected struct WorkerState(int maximumConcurrencyLevel)
    {
        private readonly ConcurrentBag<int> _workerIds = new(Enumerable.Range(0, maximumConcurrencyLevel));
        private readonly int _maximumConcurrencyLevel = maximumConcurrencyLevel;
        private int _currentDegreeOfParallelism = 0;

        public int VolatileWorkerCount => Volatile.Read(ref _currentDegreeOfParallelism);

        public WorkerStateSnapshot ClaimWorkerSlot()
        {
            // post increment, so we start at 0
            int workerId = -1;
            int original = Atomic.IncrementClampMaxFast(ref _currentDegreeOfParallelism, _maximumConcurrencyLevel);
            if (original < _maximumConcurrencyLevel)
            {
                DebugLog.WriteDiagnostic($"Attempting to claim worker slot {original}.", LogWriter.Blocking);
                SpinWait spinner = default;
                for (int i = 0; !_workerIds.TryTake(out workerId); i++)
                {
                    DebugLog.WriteWarning($"Worker slot is not yet available, spinning ({i} times so far).", LogWriter.Blocking);
                    spinner.SpinOnce();
                }
                original++;
            }
            return new WorkerStateSnapshot(workerId, original);
        }

        public void ResignWorker(ref int workerId)
        {
            DebugLog.WriteDiagnostic($"Resigning worker {workerId}.", LogWriter.Blocking);
            // pre-decrement, so we start at the maximum value - 1
            int workerCount = Interlocked.Decrement(ref _currentDegreeOfParallelism);
            _workerIds.Add(workerId);
            int[] workerIds;
            Debug.Assert(workerCount >= 0);
            Debug.Assert((workerIds = _workerIds.ToArray()) != null && workerIds.Length == workerIds.Distinct().Count());
            workerId = -1;
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
    private protected readonly ref struct WorkerStateSnapshot(int callerWorkerId, int workerCount)
    {
        [FieldOffset(0)]
        public readonly int CallerWorkerId = callerWorkerId;
        [FieldOffset(4)]
        public readonly int WorkerCount = workerCount;

        public bool CallerClaimedWorkerSlot => CallerWorkerId != -1;
    }
}
