using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.WorkloadTypes;

using CommonFlags = WorkloadStatus.CommonFlags;

internal abstract class AnonymousWorkload : AbstractWorkloadBase
{
    private protected AnonymousWorkload(WorkloadStatus status) : base(status) => Pass();

    internal override void InternalAbort()
    {
        DebugLog.WriteDiagnostic($"{this}: Forcing internal cancellation.", LogWriter.Blocking);
        // we're forcing cancellation, so we can just set the status to canceled
        if (Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Canceled, ~CommonFlags.Completed))
        {
            DebugLog.WriteDiagnostic($"{this}: Successfully forced internal cancellation.", LogWriter.Blocking);
            InternalRunContinuations();
        }
        else
        {
            DebugLog.WriteDiagnostic($"{this}: Failed to force internal cancellation. Status is '{Status}'.", LogWriter.Blocking);
        }
    }

    // Only used for cancellation. Anonymous workloads are not bound to a qdisc.
    internal override bool TryInternalBindQdisc(IQdisc qdisc)
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to bind workload to qdisc.", LogWriter.Blocking);
        if (!Atomic.TryTestAnyFlagsExchange(ref _status, WorkloadStatus.Scheduled, WorkloadStatus.Created | WorkloadStatus.Scheduled))
        {
            // this is weird. also yes, the construction of the exception message is not entirely thread-safe. sue me.
            WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Status was '{Status}' during binding attempt.");
            // can't throw on scheduler thread, so we'll just log it. The logger can throw if it wants to.
            DebugLog.WriteException(exception, LogWriter.Blocking);
            return false;
        }
        return true;
    }

    private protected abstract void ExecuteCore();

    internal override bool TryRunSynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        WorkloadStatus preTerminationStatus;
        if ((preTerminationStatus = Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled)) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            try
            {
                // execute the workload
                ExecuteCore();
                // if cancellation was requested, but the workload didn't honor it,
                // then we'll just ignore it and treat it as a successful completion
                preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
                if (preTerminationStatus == WorkloadStatus.Running)
                {
                    DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
                    return true;
                }
                // this should never happen
            }
            catch (Exception ex)
            {
                Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
                DebugLog.WriteDiagnostic($"{this}: Execution finished faulted with '{ex.GetType().Name}: {ex.Message}'", LogWriter.Blocking);
                return true;
            }
        }
        // log this occurrence as it should never happen
        WorkloadSchedulingException exception = WorkloadSchedulingException.CreateVirtual($"Workload is in an invalid state. This is a bug. Encountered Status '{preTerminationStatus}' during execution attempt.");
        DebugLog.WriteException(exception, LogWriter.Blocking);
        // notify the caller that the workload could not be executed
        // they might be awaiting the workload, so we need to set the result if it's not already set
        Interlocked.Exchange(ref _status, WorkloadStatus.Faulted);
        // in any case, we couldn't execute the workload due to some scheduling issue
        // returning false will allow the scheduler to back-track and try again from the previous state if possible
        return false;
    }
}
