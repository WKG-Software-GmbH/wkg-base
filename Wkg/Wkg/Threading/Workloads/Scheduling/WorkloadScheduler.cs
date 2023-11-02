using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Scheduling;

using CommonFlags = WorkloadStatus.CommonFlags;

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

    void INotifyWorkScheduled.OnWorkScheduled()
    {
        DebugLog.WriteDiagnostic("Workload scheduler was poked.", LogWriter.Blocking);

        // this atomic clamped increment is committing, if we have room for another worker, we must start one
        // we are not allowed to abort the operation, because that could lead to starvation
        int original = Atomic.IncrementClampMaxFast(ref _currentDegreeOfParallelism, _maximumConcurrencyLevel);
        if (original < _maximumConcurrencyLevel)
        {
            // we have room for another worker, so we'll start one
            DebugLog.WriteDiagnostic($"Successfully started a new worker: {original} -> {original + 1}.", LogWriter.Blocking);
            // do not flow the execution context to the worker
            DispatchWorkerNonCapturing();
            // we successfully started a worker, so we can exit
            return;
        }
        // we're at the max degree of parallelism, so we can exit
        DebugLog.WriteDiagnostic($"Reached maximum concurrency level: {original} >= {_maximumConcurrencyLevel}.", LogWriter.Blocking);
    }

    private void DispatchWorkerNonCapturing()
    {
        using (ExecutionContext.SuppressFlow())
        {
            ThreadPool.QueueUserWorkItem(WorkerLoop, null);
        }
    }

    protected virtual void WorkerLoop(object? state)
    {
        DebugLog.WriteInfo("Worker started.", LogWriter.Blocking);
        bool previousExecutionFailed = false;
        while (TryDequeueOrExitSafely(previousExecutionFailed, out AbstractWorkloadBase? workload))
        {
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.Status.IsOneOf(CommonFlags.Completed));
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
    protected bool TryDequeueOrExitSafely(bool previousExecutionFailed, [NotNullWhen(true)] out AbstractWorkloadBase? workload)
    {
        DebugLog.WriteDiagnostic("Worker is attempting to dequeue a workload.", LogWriter.Blocking);
        // race against scheduling threads
        while (true)
        {
            if (_rootQdisc.TryDequeueInternal(previousExecutionFailed, out workload))
            {
                DebugLog.WriteDiagnostic($"Worker successfully dequeued workload {workload}.", LogWriter.Blocking);
                // we successfully dequeued a task, return with success
                return true;
            }
            // we are about to exit, so we must decrement the worker count
            // there is no need for a clamped decrement, because every worker will only ever increment the worker count
            // one when it is started, and decrement it once when it exits
            // as long as invariants are not broken, the worker count will never be negative
            Interlocked.Decrement(ref _currentDegreeOfParallelism);
            // re-sample the queue
            DebugLog.WriteDiagnostic($"Worker found no tasks, resampling root qdisc to ensure true emptiness.", LogWriter.Blocking);
            // it is the responsibility of the qdisc implementation to ensure that this operation is thread-safe
            if (_rootQdisc.IsEmpty)
            {
                // no more tasks, exit
                DebugLog.WriteDiagnostic($"Worker found no more tasks, exiting.", LogWriter.Blocking);
                return false;
            }
            DebugLog.WriteDiagnostic($"Worker exit attempt was interrupted by new scheduling activity. Attempting to restore worker count.", LogWriter.Blocking);
            // failure case: someone else scheduled a task, possibly before we decremented the worker count,
            // so we could lose a worker here attempt abort the exit by incrementing the worker count again
            // we could be racing against a scheduling thread, or any other worker that is also trying to exit
            // we simply do a simple atomic clamped increment, and depending on that result, we either exit, or we retry
            // this is thread-safe, because we CAS first, and decide based on the result without any interleaving
            int original = Atomic.IncrementClampMaxFast(ref _currentDegreeOfParallelism, _maximumConcurrencyLevel);
            if (original >= _maximumConcurrencyLevel)
            {
                // somehow someone else comitted to creating a new worker, that's unfortunate due to the scheduling overhead
                // but they are committed now, so we must give up and exit
                DebugLog.WriteDebug($"Worker exiting after encountering maximum concurrency level {_maximumConcurrencyLevel} during restore attempt.", LogWriter.Blocking);
                return false;
            }
            DebugLog.WriteDebug($"Worker successfully recovered from termination attempt and will continue attempting to dequeue workloads.", LogWriter.Blocking);
        }
    }
}
