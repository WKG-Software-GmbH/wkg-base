using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Internals;

internal class WorkloadScheduler
{
    private readonly IQdisc _rootQdisc;
    private readonly int _maximumConcurrencyLevel;
    private int _currentDegreeOfParallelism;

    public WorkloadScheduler(IQdisc rootQdisc, int maximumConcurrencyLevel)
    {
        if (maximumConcurrencyLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrencyLevel), maximumConcurrencyLevel, "The maximum degree of parallelism must be greater than zero.");
        }
        _rootQdisc = rootQdisc;
        _maximumConcurrencyLevel = maximumConcurrencyLevel;
    }

    public int MaximumConcurrencyLevel => _maximumConcurrencyLevel;

    /// <summary>
    /// Notifies the scheduler that there is work to be done.
    /// </summary>
    internal void InternalNotify()
    {
        SpinWait spinner = default;

        while (true)
        {
            int workerCount = Volatile.Read(ref _currentDegreeOfParallelism);
            if (workerCount < _maximumConcurrencyLevel)
            {
                // we have room for another worker, so we'll start one
                int actualWorkerCount = Interlocked.CompareExchange(ref _currentDegreeOfParallelism, workerCount + 1, workerCount);
                if (actualWorkerCount == workerCount)
                {
                    // we successfully started a worker, so we can exit
                    ThreadPool.QueueUserWorkItem(WorkerLoop, null);
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
                    return;
                }
                // we gained a worker.
                // the only way this could happen is if another task was scheduled,
                // and the worker count was incremented before we could increment it.
                // this still means that we could benefit from another worker, so we'll try again
                // worst case is that we'll start a worker that will find out that it's useless because somone else already did the work.
                // in that case, the worker will end itself, and everything will be fine
                spinner.SpinOnce();
            }
            else
            {
                // we're at the max degree of parallelism, so we can exit
                return;
            }
        }
    }

    private void WorkerLoop(object? state)
    {
        bool previousExecutionFailed = false;
        while (TryDequeueOrExitSafely(previousExecutionFailed, out Workload? workload))
        {
            previousExecutionFailed = workload.TryRunSynchronously();
            Debug.Assert(workload.IsCompleted);
        }
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
    private bool TryDequeueOrExitSafely(bool previousExecutionFailed, [NotNullWhen(true)] out Workload? workload)
    {
        SpinWait spinner = default;
        // spin loop against scheduling threads
        while (true)
        {
            if (_rootQdisc.TryDequeue(previousExecutionFailed, out workload))
            {
                // we successfully dequeued a task, return with success
                return true;
            }
            int workerCountAfterExit = Interlocked.Decrement(ref _currentDegreeOfParallelism);
            int originalWorkerCount = workerCountAfterExit + 1;
            // re-sample the queue
            // it is the responsibility of the qdisc implementation to ensure that this operation is thread-safe
            if (_rootQdisc.IsEmpty)
            {
                // no more tasks, exit
                return false;
            }
            // failure case: someone else scheduled a task, possible before we decremented the worker count, so we could lose a worker here
            // attempt abort the exit by incrementing the worker count again
            // spin loop against other workers currently exiting
            while (true)
            {
                int newWorkerCount = Interlocked.CompareExchange(ref _currentDegreeOfParallelism, originalWorkerCount, workerCountAfterExit);
                if (newWorkerCount > workerCountAfterExit)
                {
                    // it's fine. They already scheduled a replacement worker, so we can exit
                    return false;
                }
                // we either successfully restored the worker count, or we lost more than one worker
                if (newWorkerCount == originalWorkerCount)
                {
                    // we successfully restored the worker count, so we can break out of the restore loop and continue the outer dequeue loop
                    break;
                }
                // some other worker beat us to restoring the worker count, so we need to try again
                workerCountAfterExit = newWorkerCount;
                originalWorkerCount = workerCountAfterExit + 1;
                // if we would violate the max degree of parallelism, we need to exit
                if (originalWorkerCount > _maximumConcurrencyLevel)
                {
                    // give up and exit
                    return false;
                }
                // continue attempts to stay alive
                // worst thing that could happen is that we have been replaced by the next iteration,
                // or that we manage to restore, stay alive, but then find out that we're useless (no tasks)
                // in any case, we'll either continure with well-defined state, or we'll exit
                spinner.SpinOnce();
            }
            spinner.SpinOnce();
        }
    }
}
