using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Internals;

internal class WorkloadScheduler : INotifyWorkScheduled
{
    private readonly IQdisc _rootQdisc;
    private readonly int _maximumConcurrencyLevel;
    private int _currentDegreeOfParallelism;

    public WorkloadScheduler(IQdisc rootQdisc, int maximumConcurrencyLevel)
    {
        if (maximumConcurrencyLevel < 1)
        {
            DebugLog.WriteWarning($"The maximum degree of parallelism must be greater than zero. The specified value was {maximumConcurrencyLevel}.", LogWriter.Blocking);
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrencyLevel), maximumConcurrencyLevel, "The maximum degree of parallelism must be greater than zero.");
        }
        _rootQdisc = rootQdisc;
        _maximumConcurrencyLevel = maximumConcurrencyLevel;
        DebugLog.WriteInfo($"Created workload scheduler with root qdisc {_rootQdisc} and maximum concurrency level {_maximumConcurrencyLevel}.", LogWriter.Blocking);
    }

    public int MaximumConcurrencyLevel => _maximumConcurrencyLevel;

    // TODO: deadlocking race condition with TryDequeueOrExitSafely!!! (when we're attempting to dispatch a new worker with one worker trying to exit)
    void INotifyWorkScheduled.OnWorkScheduled()
    {
        DebugLog.WriteDiagnostic("Workload scheduler was poked.", LogWriter.Blocking);

        SpinWait spinner = default;
        while (true)
        {
            int workerCount = Volatile.Read(ref _currentDegreeOfParallelism);
            if (workerCount < _maximumConcurrencyLevel)
            {
                DebugLog.WriteDiagnostic($"Attempting to start a new worker: {workerCount} < {_maximumConcurrencyLevel}.", LogWriter.Blocking);
                // we have room for another worker, so we'll start one
                // TODO: this is broken (DEADLOCK / worker starvation). Can't we simplify this with Atomic.IncrementClampMax?
                int actualWorkerCount = Interlocked.CompareExchange(ref _currentDegreeOfParallelism, workerCount + 1, workerCount);
                if (actualWorkerCount == workerCount)
                {
                    DebugLog.WriteDiagnostic($"Successfully started a new worker: {workerCount} -> {actualWorkerCount + 1}.", LogWriter.Blocking);
                    // do not flow the execution context to the worker
                    DispatchWorkerNonCapturing();
                    // we successfully started a worker, so we can exit
                    return;
                }
                // the worker count changed. We either lost a worker, or we gained a worker
                if (actualWorkerCount < workerCount)
                {
                    // we lost a worker.
                    // the only way this could happen is if the worker exited, because there were no more tasks
                    // however, we just added a task, so we know that there are tasks
                    // we also know that that the worker will re-sample the queue, so we trust that the worker knows what it's doing
                    // we can exit
                    DebugLog.WriteDiagnostic($"Lost a worker: {workerCount} -> {actualWorkerCount}.", LogWriter.Blocking);
                    return;
                }
                // we gained a worker.
                // the only way this could happen is if another task was scheduled,
                // and the worker count was incremented before we could increment it.
                // this still means that we could benefit from another worker, so we'll try again
                // worst case is that we'll start a worker that will find out that it's useless because somone else already did the work.
                // in that case, the worker will end itself, and everything will be fine
                DebugLog.WriteDiagnostic($"Gained a worker: {workerCount} -> {actualWorkerCount}. Spinning for retry.", LogWriter.Blocking);
                spinner.SpinOnce();
            }
            else
            {
                // we're at the max degree of parallelism, so we can exit
                DebugLog.WriteDiagnostic($"Reached maximum concurrency level: {workerCount} >= {_maximumConcurrencyLevel}.", LogWriter.Blocking);
                return;
            }
        }
    }

    private void DispatchWorkerNonCapturing()
    {
        bool restoreFlow = false;
        try
        {
            if (!ExecutionContext.IsFlowSuppressed())
            {
                ExecutionContext.SuppressFlow();
                restoreFlow = true;
            }
            ThreadPool.QueueUserWorkItem(WorkerLoop, null);
        }
        finally
        {
            if (restoreFlow)
            {
                ExecutionContext.RestoreFlow();
            }
        }
    }

    private void WorkerLoop(object? state)
    {
        DebugLog.WriteInfo("Worker started.", LogWriter.Blocking);
        bool previousExecutionFailed = false;
        while (TryDequeueOrExitSafely(previousExecutionFailed, out AbstractWorkloadBase? workload))
        {
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.IsCompleted);
            workload.InternalRunContinuations();
        }
        DebugLog.WriteInfo("Worker exited.", LogWriter.Blocking);
    }

    /// <summary>
    /// Attempts to dequeue a workload from the root qdisc, and if that fails, attempts to clean up the worker thread and establish a well-defined state with one less worker.
    /// </summary>
    /// <param name="previousExecutionFailed"><see langword="true"/> if the previous workload execution failed; <see langword="false"/> if the previous workload execution succeeded. Instructs the underlying qdisc to back track to the previous state if possible.</param>
    /// <param name="workload">The dequeued <see cref="Workload"/>, or <see langword="null"/> if the worker should exit.</param>
    /// <returns><see langword="true"/> if a workload was dequeued, <see langword="false"/> if the worker should exit.</returns>
    /// <remarks>
    /// If this method returns <see langword="false"/>, the worker must exit in order to respect the max degree of parallelism.
    /// </remarks>
    // TODO: deadlocking race condition with OnWorkScheduled!!! (when we're attempting to restore the worker count with 1 worker in total)
    private bool TryDequeueOrExitSafely(bool previousExecutionFailed, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        DebugLog.WriteDiagnostic("Worker is attempting to dequeue a workload.", LogWriter.Blocking);
        SpinWait spinner = default;
        // spin loop against scheduling threads
        while (true)
        {
            if (_rootQdisc.TryDequeueInternal(previousExecutionFailed, out workload))
            {
                DebugLog.WriteDiagnostic($"Worker successfully dequeued workload {workload}.", LogWriter.Blocking);
                // we successfully dequeued a task, return with success
                return true;
            }
            // IDEA: we could introduce a non-blocking critical section here.
            // Scheduling threads will have to wait for the worker to figure out if it wants to exit or not.
            int workerCountAfterExit = Interlocked.Decrement(ref _currentDegreeOfParallelism);
            // re-sample the queue
            DebugLog.WriteDiagnostic($"Worker found no tasks, resampling root qdisc to ensure true emptiness.", LogWriter.Blocking);
            // it is the responsibility of the qdisc implementation to ensure that this operation is thread-safe
            if (_rootQdisc.IsEmpty)
            {
                // no more tasks, exit
                DebugLog.WriteDiagnostic($"Worker found no more tasks, exiting.", LogWriter.Blocking);
                return false;
            }
            // failure case: someone else scheduled a task, possible before we decremented the worker count,
            // so we could lose a worker here attempt abort the exit by incrementing the worker count again
            // spin loop against other workers currently exiting
            while (true)
            {
                // WTF: why not just do an atomic clamped increment here?
                int restoreTarget = workerCountAfterExit + 1;
                int preRestoreWorkerCount = Interlocked.CompareExchange(ref _currentDegreeOfParallelism, restoreTarget, workerCountAfterExit);
                DebugLog.WriteDiagnostic($"Worker attempting to restore worker count: {workerCountAfterExit} -> {restoreTarget}. Actual worker count: {preRestoreWorkerCount}.", LogWriter.Blocking);
                // originalWorkerCount = 1
                // workerCountAfterExit = 0
                // preRestoreWorkerCount = 0
                // ==> successfull restore
                if (preRestoreWorkerCount > workerCountAfterExit)
                {
                    // it's fine. They already scheduled a replacement worker, so we can exit
                    DebugLog.WriteDiagnostic($"Worker exiting after replacement worker was scheduled.", LogWriter.Blocking);
                    return false;
                }
                // we either successfully restored the worker count, or we lost more than one worker
                if (preRestoreWorkerCount == workerCountAfterExit)
                {
                    // the worker count before our restore attempt was the one we expected, so we successfully
                    // restored to the original worker count. we can break out of the restore loop, spin a few
                    // cycles and continue resampling in the outer dequeue loop.
                    DebugLog.WriteDiagnostic($"Worker successfully restored worker count to {restoreTarget}. Continuing dequeue loop.", LogWriter.Blocking);
                    break;
                }
                // some other worker beat us to restoring the worker count, so we need to try again
                // attempt to stay alive!
                workerCountAfterExit = preRestoreWorkerCount;
                restoreTarget = workerCountAfterExit + 1;
                // if we would violate the max degree of parallelism, we need to exit
                // TODO: this is risky!!!!
                if (restoreTarget > _maximumConcurrencyLevel)
                {
                    // give up and exit
                    DebugLog.WriteDiagnostic($"Worker exiting after encountering maximum concurrency level {_maximumConcurrencyLevel} during restore attempt.", LogWriter.Blocking);
                    return false;
                }
                // continue attempts to stay alive
                // worst thing that could happen is that we have been replaced by the next iteration,
                // or that we manage to restore, stay alive, but then find out that we're useless (no tasks)
                // in any case, we'll either continure with well-defined state, or we'll exit
                DebugLog.WriteDiagnostic($"Worker spinning for retry during restore attempt.", LogWriter.Blocking);
                spinner.SpinOnce();
            }
            DebugLog.WriteDiagnostic($"Worker spinning for retry while attempting to dequeue workload.", LogWriter.Blocking);
            spinner.SpinOnce();
        }
    }
}
