using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClassfulWorkloadFactory<THandle> : AbstractClasslessWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClassfulWorkloadFactory(IClassfulQdisc<THandle> root, AnonymousWorkloadPool? pool) : base(root, pool)
    {
    }

    private protected IClassfulQdisc<THandle> ClassfulRoot => Unsafe.As<IClasslessQdisc<THandle>, IClassfulQdisc<THandle>>(ref RootRef);

    public virtual void Schedule(in THandle handle, Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkload(action);
        ScheduleCore(in handle, workload);
    }

    public virtual Workload Schedule(in THandle handle, Action<CancellationFlag> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload workload = new(action);
        ScheduleCore(in handle, workload);
        return workload;
    }

    public virtual Workload<TResult> Schedule<TState, TResult>(in THandle handle, Func<CancellationFlag, TResult> func)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload<TResult> workload = new(func);
        ScheduleCore(in handle, workload);
        return workload;
    }

    private protected virtual void ScheduleCore(in THandle handle, AbstractWorkloadBase workload)
    {
        if (_root.Handle.Equals(handle))
        {
            _root.Enqueue(workload);
        }
        else if (ClassfulRoot.TryFindChild(in handle, out IClasslessQdisc<THandle>? child))
        {
            child.Enqueue(workload);
        }
        else
        {
            throw new WorkloadSchedulingException($"The workload could not be scheduled: no child qdisc with handle {handle} was found.");
        }
    }
}
