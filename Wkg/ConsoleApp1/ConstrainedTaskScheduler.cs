using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Collections.Concurrent;

namespace ConsoleApp1;

public class ConstrainedTaskScheduler : TaskScheduler
{
    private readonly ConcurrentQueue<Task> _tasks = new();
    private readonly ConcurrentHashSet<Task> _scheduledTasks = new();

    private readonly int _maxDegreeOfParallelism;
    private int _currentDegreeOfParallelism;

    [ThreadStatic]
    private static bool _currentThreadIsProcessingItems;

    public ConstrainedTaskScheduler(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), maxDegreeOfParallelism, "The maximum degree of parallelism must be greater than zero.");
        }
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
    }

    /// <inheritdoc/>
    public override int MaximumConcurrencyLevel => _maxDegreeOfParallelism;

    /// <inheritdoc/>
    // use snapshot enumeration of the queue
    protected override IEnumerable<Task>? GetScheduledTasks() => _tasks.AsEnumerable();

    /// <inheritdoc/>
    protected override void QueueTask(Task task)
    {
        if (!_scheduledTasks.Add(task))
        {
            throw new InvalidOperationException("Task was already scheduled.");
        }
        _tasks.Enqueue(task);

        SpinWait spinner = default;

        while (true)
        {
            int workerCount = Volatile.Read(ref _currentDegreeOfParallelism);
            if (workerCount < _maxDegreeOfParallelism)
            {
                // we have room for another worker, so we'll start one
                int actualWorkerCount = Interlocked.CompareExchange(ref _currentDegreeOfParallelism, workerCount + 1, workerCount);
                if (actualWorkerCount == workerCount)
                {
                    // we successfully started a worker, so we can exit
                    ThreadPool.UnsafeQueueUserWorkItem(WorkerLoop, null);
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

    /// <inheritdoc/>
    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (!_currentThreadIsProcessingItems)
        {
            // we are not a worker thread, so we are not allowed to execute tasks inline
            return false;
        }
        if (taskWasPreviouslyQueued && !_scheduledTasks.TryRemove(task))
        {
            // the task was already scheduled, but it was cancelled or already executed
            return false;
        }
        // we are a worker thread, so we are allowed to execute tasks inline
        return TryExecuteTask(task);
    }

    private void WorkerLoop(object? state)
    {
        _currentThreadIsProcessingItems = true;
        try
        {
            while (TryDequeueOrExitSafely(out Task? task))
            {
                TryExecuteTask(task);
            }
        }
        finally
        {
            _currentThreadIsProcessingItems = false;
        }
    }

    /// <summary>
    /// Attempts to dequeue a task from the queue, and if that fails, attempts to exit the worker loop in a well-defined state by ensuring that the internal state remains consistent.
    /// </summary>
    /// <param name="task">The dequeued task, or <see langword="null"/> if the worker should exit.</param>
    /// <returns><see langword="true"/> if a task was dequeued, <see langword="false"/> if the worker should exit.</returns>
    /// <remarks>
    /// If this method returns <see langword="false"/>, the worker must exit in order to respect the max degree of parallelism.
    /// </remarks>
    private bool TryDequeueOrExitSafely([NotNullWhen(true)] out Task? task)
    {
        SpinWait spinner = default;
        // spin loop against scheduling threads
        while (true)
        {
            if (_tasks.TryDequeue(out task))
            {
                // we need to check if the task was cancelled
                if (!_scheduledTasks.TryRemove(task))
                {
                    // this task was cancelled, ignore it and continue
                    continue;
                }
                // we successfully dequeued a task, return with success
                return true;
            }
            int workerCountAfterExit = Interlocked.Decrement(ref _currentDegreeOfParallelism);
            int originalWorkerCount = workerCountAfterExit + 1;
            // re-sample the queue
            // this operation is thread-safe, because the underlying collection is thread-safe
            int taskCount = _tasks.Count;
            if (taskCount == 0)
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
                if (originalWorkerCount > _maxDegreeOfParallelism)
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

    /// <inheritdoc/>
    protected override bool TryDequeue(Task task) =>
        // soft delete the task from the scheduler queue
        // once the task is processed by a worker, it will be discarded and not executed
        _scheduledTasks.TryRemove(task);
}
