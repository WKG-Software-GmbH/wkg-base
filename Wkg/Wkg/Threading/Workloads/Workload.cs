using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

[DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
public class Workload
{
    private uint _status;
    internal readonly Action<CancellationFlag> _action;
    private ValueTask<WorkloadResult>? _task;
    private IQdisc? _qdisc;
    private static readonly IQdisc _qdiscCompletionSentinel = new QdiscCompletionSentinel();
    private object? _state;

    private Workload(Action<CancellationFlag> action, WorkloadStatus status)
    {
        _action = action;
        _status = status;
    }

    /// <summary>
    /// Gets the unique identifier of this workload.
    /// </summary>
    public ulong Id { get; } = WorkloadIdGenerator.Generate();

    // TODO: overload without cancellation (would allow us to simplify in cases where we don't need cancellation)
    // TODO: we could even make the uncancellable type a struct, as the caller doesn't need to keep a reference to it
    public static Workload Create(Action<CancellationFlag> action) =>
        CreateInternalUnsafe(action, WorkloadStatus.Created);

    internal static Workload CreateInternalUnsafe(Action<CancellationFlag>? action, WorkloadStatus status) =>
        new(action!, status);

    public WorkloadStatus Status => Volatile.Read(ref _status);

    public bool IsCompleted => Status.IsOneOf(CommonFlags.Completed);

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
        DebugLog.WriteDiagnostic($"{this}: Attempting to cancel workload.", LogWriter.Blocking);
        // fast and easy path
        if (IsCompleted)
        {
            // already completed, nothing to do
            DebugLog.WriteDiagnostic($"{this}: Workload is already completed, nothing to do.", LogWriter.Blocking);
            return false;
        }
        // try to abort from pre-execution state
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Canceled, CommonFlags.PreExecution))
        {
            DebugLog.WriteDiagnostic($"{this}: Successfully canceled workload from pre-execution state.", LogWriter.Blocking);
            // attempt to remove the workload from the qdisc
            IQdisc? qdisc, resampledQdisc;
            do
            {
                qdisc = Volatile.Read(ref _qdisc);
                if (qdisc is null)
                {
                    DebugLog.WriteDiagnostic($"{this}: Workload is not bound to a qdisc, nothing to do.", LogWriter.Blocking);
                    // no qdisc, we're done
                    return true;
                }
                // attempt to remove the workload from the qdisc
                if (qdisc.TryRemoveInternal(this))
                {
                    // we successfully removed the workload from the qdisc, we're done
                    // detach from the qdisc (only after we successfully removed the workload from it)
                    UnbindQdiscUnsafe();
                    DebugLog.WriteDiagnostic($"{this}: Successfully removed workload from qdisc.", LogWriter.Blocking);
                    return true;
                }
                // we failed to remove the workload from the qdisc.
                // either the qdisc doesn't support removal of workloads, or the workload was already dequeued into another qdisc
                // we'll try to resample the qdisc and try again
                resampledQdisc = Volatile.Read(ref _qdisc);
            } while (!ReferenceEquals(qdisc, resampledQdisc));
            // we failed to remove the workload from the qdisc, so the qdisc must not support removal of workloads
            // this is valid. the workload will just be ignored when it is dequeued
            DebugLog.WriteDiagnostic($"{this}: Failed to remove workload from qdisc. The qdisc does not support removal of workloads.", LogWriter.Blocking);
            return true;
        }
        // didn't work. We're either running or invalid, request cancellation
        // we can only request cancellation but can't guarantee it will be honored
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.CancellationRequested, WorkloadStatus.Running) == WorkloadStatus.Running)
        {
            // it is the responsibility of the workload to check for cancellation
            DebugLog.WriteDiagnostic($"{this}: Successfully requested cancellation of workload.", LogWriter.Blocking);
            return true;
        }
        DebugLog.WriteDiagnostic($"{this}: Failed to request cancellation of workload.", LogWriter.Blocking);
        // invalid state
        return false;
    }

    /// <summary>
    /// Attempts to bind the workload to the specified qdisc.
    /// </summary>
    /// <remarks>
    /// The workload should be bound first, before being enqueued into the qdisc.
    /// </remarks>
    /// <param name="qdisc">The qdisc to bind to.</param>
    /// <returns><see langword="true"/> if the workload was successfully bound to the qdisc; <see langword="false"/> if the workload has already completed, is in an unbindable state, or another binding operation was faster.</returns>
    internal bool TryInternalBindQdisc(IQdisc qdisc)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to bind workload to qdisc.", LogWriter.Blocking);
        // sample the current qdisc
        IQdisc? current = Volatile.Read(ref _qdisc);
        // if the sentine is set, we cannot bind
        if (ReferenceEquals(current, _qdiscCompletionSentinel))
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to bind workload to qdisc. The qdisc is in a completion state and the sentinel is set.", LogWriter.Blocking);
            return false;
        }
        // attempt to transition to the scheduled state
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Scheduled, WorkloadStatus.Created | WorkloadStatus.Scheduled))
        {
            // successfully transitioned to the scheduled state
            // now just try to CAS the qdisc in there
            // if we fail, then another thread was faster and we should just return
            // it is also possible that the workload is already running, or has just been canceled
            // in any way, we try our best, and if it doesn't work, we just give up
            return ReferenceEquals(Interlocked.CompareExchange(ref _qdisc, current, qdisc), current);
        }
        // this is weird. also yes, the construction of the exception message is not entirely thread-safe. sue me.
        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Status was '{Status}' during binding attempt.");
        // can't throw on scheduler thread, so we'll just log it. The logger can throw if it wants to.
        DebugLog.WriteException(exception, LogWriter.Blocking);
        return false;
    }

    /// <summary>
    /// Attempts to unbind the workload from the specified qdisc. 
    /// This method requires that this workload has already been dequeued from the qdisc.
    /// </summary>
    private void UnbindQdiscUnsafe() => Volatile.Write(ref _qdisc, _qdiscCompletionSentinel);

    internal bool InternalTryMarkAborted() =>
        Interlocked.CompareExchange(ref _status, WorkloadStatus.Canceled, WorkloadStatus.CancellationRequested) == WorkloadStatus.CancellationRequested;

    /// <summary>
    /// Attempts to execute the action associated with this workload.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was executed and is in any of the terminal states; <see langword="false"/> if the workload could not be executed.</returns>
    internal bool TryRunSynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        // TODO: what about the execution context?
        // we don't want to capture the execution context of the thread starting the worker loop in the scheduler,
        // but the one of the caller scheduling the workload in the first place
        WorkloadStatus status;
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            UnbindQdiscUnsafe();
            try
            {
                // execute the workload
                _action(new CancellationFlag(this));
                // if cancellation was requested, but the workload didn't honor it,
                // then we'll just ignore it and treat it as a successful completion
                status = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
                if (status.IsOneOf(CommonFlags.WillCompleteSuccessfully))
                {
                    _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCompleted());
                    DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
                }
                else if (status == WorkloadStatus.Canceled)
                {
                    _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());
                    DebugLog.WriteDiagnostic($"{this}: Execution was canceled.", LogWriter.Blocking);
                }
                else
                {
                    // this should never happen
                    DebugLog.WriteWarning($"{this}: Workload is in an invalid state. This is a bug. Status was '{status}' after execution.", LogWriter.Blocking);
                    goto FAILURE;
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Canceled);
                _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());
                DebugLog.WriteDiagnostic($"{this}: Execution was canceled with an OperationCanceledException.", LogWriter.Blocking);
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateFaulted(ex));
                DebugLog.WriteDiagnostic($"{this}: Execution finished faulted with '{ex.GetType().Name}: {ex.Message}'", LogWriter.Blocking);
            }
            return true;
        }
    FAILURE:
        // not all qdiscs may support removal of canceled workloads)
        if ((status = Volatile.Read(ref _status)) != WorkloadStatus.Canceled)
        {
            // log this occurrence as it should never happen
            WorkloadSchedulingException exception = new($"Workload is in an invalid state. This is a bug. Encountered Status '{status}' during execution attempt.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
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

    #region overrides

    /// <inheritdoc/>
    public override string ToString() => 
        $"Workload {Id} ({Status})";

    #endregion overrides
}

file class QdiscCompletionSentinel : IQdisc
{
    private const string _message = "Internal error: Qdisc completion sentinel should never be accessed. This is a bug. Please report this issue.";
    bool IQdisc.IsEmpty => ThrowHelper<bool>();
    int IQdisc.Count => ThrowHelper<int>();
    bool IQdisc.TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out Workload? workload) => (workload = null) == null && ThrowHelper<bool>();
    bool IQdisc.TryRemoveInternal(Workload workload) => ThrowHelper<bool>();
    void IQdisc.InternalInitialize(INotifyWorkScheduled parentScheduler) => ThrowHelper<bool>();
    void IQdisc.Complete() => ThrowHelper<bool>();

    [DoesNotReturn]
    [StackTraceHidden]
    private static T ThrowHelper<T>() => throw new WorkloadSchedulingException(_message);
}

file static class WorkloadIdGenerator
{
    private static ulong _nextId;

    public static ulong Generate() => Interlocked.Increment(ref _nextId);
}