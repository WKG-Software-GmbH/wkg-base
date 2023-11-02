using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.WorkloadTypes;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClassfulWorkloadFactoryWithDI<THandle> : AbstractClassfulWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClassfulWorkloadFactoryWithDI(IClassfulQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public virtual void Classify<TState>(TState state, Action<IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImplWithDI(action);
        ClassifyCore(state, workload);
    }

    public virtual void Classify<TState>(TState state, Action<TState, IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithDIAndState<TState>(state, action);
        ClassifyCore(state, workload);
    }

    public virtual Workload ClassifyAsync<TState>(TState state, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, action, options, CancellationToken.None);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ClassifyAsync(state, action, null, cancellationToken);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDI(action, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, action, options, CancellationToken.None);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ClassifyAsync(state, action, null, cancellationToken);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDIAndState<TState>(state, action, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ClassifyAsync(state, func, null, cancellationToken);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDI<TResult>(func, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ClassifyAsync(state, func, null, cancellationToken);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDIAndState<TState, TResult>(state, func, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual void Schedule(in THandle handle, Action<IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImplWithDI(action);
        ScheduleCore(in handle, workload);
    }

    public virtual void Schedule<TState>(in THandle handle, TState state, Action<TState, IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithDIAndState<TState>(state, action);
        ScheduleCore(in handle, workload);
    }

    public virtual Workload ScheduleAsync(in THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, action, CancellationToken.None);

    public virtual Workload ScheduleAsync(in THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, action, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload ScheduleAsync(in THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDI(action, options, cancellationToken);
        ScheduleCore(in handle, workload);
        return workload;
    }

    public virtual Workload ScheduleAsync<TState>(in THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, state, action, CancellationToken.None);

    public virtual Workload ScheduleAsync<TState>(in THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, state, action, null, cancellationToken);

    public virtual Workload ScheduleAsync<TState>(in THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDIAndState<TState>(state, action, options, cancellationToken);
        ScheduleCore(in handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, func, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(in THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDI<TResult>(func, options, cancellationToken);
        ScheduleCore(in handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(in THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(in handle, state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(in THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(in handle, state, func, null, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(in THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDIAndState<TState, TResult>(state, func, options, cancellationToken);
        ScheduleCore(in handle, workload);
        return workload;
    }
}
