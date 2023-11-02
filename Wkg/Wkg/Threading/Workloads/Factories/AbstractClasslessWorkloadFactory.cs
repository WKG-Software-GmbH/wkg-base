using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClasslessWorkloadFactory<THandle> : WorkloadFactory<THandle> where THandle : unmanaged
{
    private protected AbstractClasslessWorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public virtual void Schedule(Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImpl(action);
        ScheduleCore(workload);
    }

    public virtual void Schedule<TState>(TState state, Action<TState> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithState<TState>(state, action);
        ScheduleCore(workload);
    }

    public virtual Workload ScheduleAsync(Action<CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(action, options, CancellationToken.None);

    public virtual Workload ScheduleAsync(Action<CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(action, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload ScheduleAsync(Action<CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImpl(action, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(state, action, options, CancellationToken.None);

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(state, action, default, cancellationToken);

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithState<TState>(state, action, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(func, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImpl<TResult>(func, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(state, func, default, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithState<TState, TResult>(state, func, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    private protected virtual void ScheduleCore(AbstractWorkloadBase workload) =>
        _root.Enqueue(workload);
}
