using Wkg.Logging;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

public class Workload
{
    private uint _status;
    internal readonly Action<CancellationFlag> _action;
    private ValueTask<WorkloadResult>? _task;

    private Workload(Action<CancellationFlag> action, WorkloadStatus status)
    {
        _action = action;
        _status = status;
    }

    // TODO: overload without cancellation (would allow us to simplify in cases where we don't need cancellation)
    // TODO: we could even make the uncancellable type a struct, as the caller doesn't need to keep a reference to it
    public static Workload Create(Action<CancellationFlag> action) =>
        CreateInternalUnsafe(action, WorkloadStatus.Created);

    internal static Workload CreateInternalUnsafe(Action<CancellationFlag>? action, WorkloadStatus status) =>
        new(action!, status);

    public WorkloadStatus Status => Volatile.Read(ref _status);

    public bool IsCompleted => Status.IsOneOf(WorkloadStatus.InternalCompletionMask);

    public WorkloadResult GetResult()
    {
        // TODO: via awaiter / awaitable
        return default;
    }

    /// <summary>
    /// Attempts to transition the workload to the <see cref="WorkloadStatus.Canceled"/> state.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was successfully canceled; <see langword="false"/> if the workload has already completed or is in an uncancelable state.</returns>
    public bool TryCancel()
    {
        // fast and easy path
        if (IsCompleted)
        {
            return false;
        }
        // try to abort from pre-execution state
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Canceled, WorkloadStatus.Created | WorkloadStatus.Scheduled))
        {
            // TODO: remove from scheduler
            return true;
        }
        // didn't work. We're either running or invalid, request cancellation
        // we can only request cancellation but can't guarantee it will be honored
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.CancellationRequested, WorkloadStatus.Running) == WorkloadStatus.Running)
        {
            // it is the responsibility of the workload to check for cancellation
            return true;
        }
        // invalid state
        return false;
    }

    internal bool InternalTryMarkAborted() =>
        Interlocked.CompareExchange(ref _status, WorkloadStatus.Canceled, WorkloadStatus.CancellationRequested) == WorkloadStatus.CancellationRequested;

    /// <summary>
    /// Attempts to execute the action associated with this workload.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was executed and is in any of the terminal states; <see langword="false"/> if the workload could not be executed.</returns>
    internal bool TryRunSynchronously()
    {
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled) == WorkloadStatus.Scheduled)
        {
            try
            {
                _action(new CancellationFlag(this));
                if (Interlocked.CompareExchange(ref _status, WorkloadStatus.RanToCompletion, WorkloadStatus.Running) == WorkloadStatus.Running)
                {
                    _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCompleted());
                }
                else if (Status == WorkloadStatus.Canceled)
                {
                    _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());
                }
                else
                {
                    // this should never happen
                    goto FAILURE;
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Canceled);
                _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateFaulted(ex));
            }
            return true;
        }
    FAILURE:
        // not all qdiscs may support removal of canceled workloads)
        WorkloadStatus status;
        if ((status = Volatile.Read(ref _status)) != WorkloadStatus.Canceled)
        {
            // log this occurrence as it should never happen
            WorkloadSchedulingException exception = new($"Workload is in an invalid state. This is a bug. Status was '{status}' during execution attempt.");
            Log.WriteException(exception, LogWriter.Blocking);
            // notify the caller that the workload could not be executed
            // they might be awaiting the workload, so we need to set the result if it's not already set
            if (!_task.HasValue)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateFaulted(exception));
            }
        }
        // in any case, we couldn't execute the workload due to some scheduling issue
        // returning false will allow the scheduler to back-track and try again from the previous state if possible
        return false;
    }
}
