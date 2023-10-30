using System;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

internal class AnonymousWorkload : AbstractWorkloadBase
{
    protected Action _action;

    internal AnonymousWorkload(Action action) : this(WorkloadStatus.Created, action) => Pass();

    internal AnonymousWorkload(WorkloadStatus status, Action action) : base(status)
    {
        _action = action;
    }

    internal override void InternalRunContinuations() => Pass();

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

    internal override bool TryRunSynchronously()
    {
        DebugLog.WriteDiagnostic($"{this}: Attempting to execute workload.", LogWriter.Blocking);
        // TODO: what about the execution context?
        // we don't want to capture the execution context of the thread starting the worker loop in the scheduler,
        // but the one of the caller scheduling the workload in the first place
        WorkloadStatus preTerminationStatus;
        if ((preTerminationStatus = Interlocked.CompareExchange(ref _status, WorkloadStatus.Running, WorkloadStatus.Scheduled)) == WorkloadStatus.Scheduled)
        {
            // we successfully transitioned to the running state
            DebugLog.WriteDiagnostic($"{this}: Successfully transitioned to running state.", LogWriter.Blocking);
            try
            {
                // execute the workload
                _action();
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

internal class PooledAnonomousWorkload : AnonymousWorkload
{
    private readonly AnonymousWorkloadPool _pool;

    internal PooledAnonomousWorkload(AnonymousWorkloadPool pool) : base(null!)
    {
        _pool = pool;
    }

    internal override void InternalRunContinuations()
    {
        Volatile.Write(ref _action, null!);
        Volatile.Write(ref _status, WorkloadStatus.Pooled);
        _pool.Return(this);
    }

    internal void Initialize(Action action)
    {
        Volatile.Write(ref _action, action);
        Volatile.Write(ref _status, WorkloadStatus.Created);
    }
}