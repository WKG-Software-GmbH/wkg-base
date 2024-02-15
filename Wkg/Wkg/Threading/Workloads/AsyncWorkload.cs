using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

public abstract class AsyncWorkload : AwaitableWorkload
{
    private protected AsyncWorkload(WorkloadStatus status, WorkloadContextOptions continuationOptions, CancellationToken cancellationToken) 
        : base(status, continuationOptions, cancellationToken) => Pass();

    internal override bool TryRunSynchronously() => TryRunAsynchronously().AsTask().GetAwaiter().GetResult();

    private protected abstract Task<WorkloadStatus> TryExecuteUnsafeCoreAsync();

    internal async ValueTask<bool> TryRunAsynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        WorkloadStatus preTerminationStatus;
        if (Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            UnbindQdiscUnsafe();
            try
            {
                preTerminationStatus = await TryExecuteUnsafeCoreAsync().ConfigureAwait(continueOnCapturedContext: false);
                if (preTerminationStatus != WorkloadStatus.AsyncSuccess)
                {
                    // this should never happen
                    DebugLog.WriteWarning($"{this}: Workload is in an invalid state. This is a bug. Status was '{preTerminationStatus}' after execution.", LogWriter.Blocking);
                    goto FAILURE;
                }
            }
            catch (WorkloadCanceledException)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Canceled);
                SetCanceledResultUnsafe();
                DebugLog.WriteDiagnostic($"{this}: Execution was canceled with a {nameof(WorkloadCanceledException)}.", LogWriter.Blocking);
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
            if (!IsCompleted)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                SetFaultedResultUnsafe(exception);
            }
        }
        // in any case, we couldn't execute the workload due to some scheduling issue
        // returning false will allow the scheduler to back-track and try again from the previous state if possible
        return false;
    }

    private protected sealed override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus) => throw new NotSupportedException();
}
