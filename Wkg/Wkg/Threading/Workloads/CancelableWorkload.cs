using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

/// <summary>
/// Represents a workload that can be canceled.
/// </summary>
public abstract class CancelableWorkload : AbstractWorkloadBase
{
    private protected static readonly IQdisc _qdiscCompletionSentinel = new QdiscCompletionSentinel();
    private protected IQdisc? _qdisc;

    private protected CancelableWorkload(WorkloadStatus status) : base(status)
    {
    }

    private protected abstract bool IsResultSet { get; }

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

    internal bool InternalTryMarkAborted() =>
        Interlocked.CompareExchange(ref _status, WorkloadStatus.Canceled, WorkloadStatus.CancellationRequested) == WorkloadStatus.CancellationRequested;

    internal override bool TryInternalBindQdisc(IQdisc qdisc)
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

    internal override void InternalMarkAsFinalized() => Pass();

    private protected abstract bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus);

    private protected abstract void SetFaultedResultUnsafe(Exception ex);

    private protected abstract void SetCanceledResultUnsafe();

    /// <inheritdoc/>
    internal override bool TryRunSynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        // TODO: what about the execution context?
        // we don't want to capture the execution context of the thread starting the worker loop in the scheduler,
        // but the one of the caller scheduling the workload in the first place
        WorkloadStatus preTerminationStatus;
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            UnbindQdiscUnsafe();
            try
            {
                if (!TryExecuteUnsafeCore(out preTerminationStatus))
                {
                    // this should never happen
                    DebugLog.WriteWarning($"{this}: Workload is in an invalid state. This is a bug. Status was '{preTerminationStatus}' after execution.", LogWriter.Blocking);
                    goto FAILURE;
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Canceled);
                SetCanceledResultUnsafe();
                DebugLog.WriteDiagnostic($"{this}: Execution was canceled with an OperationCanceledException.", LogWriter.Blocking);
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                SetFaultedResultUnsafe(ex);
                DebugLog.WriteDiagnostic($"{this}: Execution finished faulted with '{ex.GetType().Name}: {ex.Message}'", LogWriter.Blocking);
            }
            return true;
        }
    FAILURE:
        // not all qdiscs may support removal of canceled workloads)
        if ((preTerminationStatus = Volatile.Read(ref _status)) != WorkloadStatus.Canceled)
        {
            // log this occurrence as it should never happen
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Encountered Status '{preTerminationStatus}' during execution attempt.");
            DebugLog.WriteException(exception, LogWriter.Blocking);
            // notify the caller that the workload could not be executed
            // they might be awaiting the workload, so we need to set the result if it's not already set
            if (!IsResultSet)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                SetFaultedResultUnsafe(exception);
            }
        }
        // in any case, we couldn't execute the workload due to some scheduling issue
        // returning false will allow the scheduler to back-track and try again from the previous state if possible
        return false;
    }

    /// <summary>
    /// Attempts to unbind the workload from the specified qdisc. 
    /// This method requires that this workload has already been dequeued from the qdisc.
    /// </summary>
    private void UnbindQdiscUnsafe() => Volatile.Write(ref _qdisc, _qdiscCompletionSentinel);
}
