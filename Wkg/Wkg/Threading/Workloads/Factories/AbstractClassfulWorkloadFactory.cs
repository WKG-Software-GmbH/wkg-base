using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.WorkloadTypes;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClassfulWorkloadFactory<THandle> : AbstractClasslessWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClassfulWorkloadFactory(IClassfulQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    private protected IClassfulQdisc<THandle> ClassfulRoot => Unsafe.As<IClasslessQdisc<THandle>, IClassfulQdisc<THandle>>(ref RootRef);

    public virtual void Schedule(in THandle handle, Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImpl(action);
        ScheduleCore(in handle, workload);
    }

    public virtual Workload ScheduleAsync(in THandle handle, Action<CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, action, CancellationToken.None);

    public virtual Workload ScheduleAsync(in THandle handle, Action<CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, action, null, cancellationToken);

    public virtual Workload ScheduleAsync(in THandle handle, Action<CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImpl(action, options, cancellationToken);
        ScheduleCore(in handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, func, null, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImpl<TResult>(func, options, cancellationToken);
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
