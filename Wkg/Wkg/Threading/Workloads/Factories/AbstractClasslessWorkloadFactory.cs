using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClasslessWorkloadFactory<THandle> : WorkloadFactory<THandle> where THandle : unmanaged
{
    private protected AbstractClasslessWorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPool? pool) : base(root, pool)
    {
    }

    public virtual void Schedule(Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkload(action);
        ScheduleCore(workload);
    }

    public virtual Workload Schedule(Action<CancellationFlag> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload workload = new(action);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload<TResult> Schedule<TResult>(Func<CancellationFlag, TResult> func)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload<TResult> workload = new(func);
        ScheduleCore(workload);
        return workload;
    }

    private protected virtual void ScheduleCore(AbstractWorkloadBase workload) =>
        _root.Enqueue(workload);
}
