using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClasslessWorkloadFactoryWithDI<THandle> : AbstractClasslessWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClasslessWorkloadFactoryWithDI(IClassifyingQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    public virtual void Schedule(Action<IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImplWithDI(action);
        ScheduleCore(workload);
    }

    public virtual void Schedule<TState>(TState state, Action<TState, IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithDIAndState<TState>(state, action);
        ScheduleCore(workload);
    }

    public virtual Workload ScheduleAsync(Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(action, options, CancellationToken.None);

    public virtual Workload ScheduleAsync(Action<IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(action, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload ScheduleAsync(Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDI(action, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(state, action, options, CancellationToken.None);

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(state, action, default, cancellationToken);

    public virtual Workload ScheduleAsync<TState>(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDIAndState<TState>(state, action, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(func, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDI<TResult>(func, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(state, func, default, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDIAndState<TState, TResult>(state, func, options, cancellationToken);
        ScheduleCore(workload);
        return workload;
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

    public virtual void Schedule(THandle handle, Action<IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImplWithDI(action);
        ScheduleCore(handle, workload);
    }

    public virtual void Schedule<TState>(THandle handle, TState state, Action<TState, IWorkloadServiceProvider> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithDIAndState<TState>(state, action);
        ScheduleCore(handle, workload);
    }

    public virtual Workload ScheduleAsync(THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, action, CancellationToken.None);

    public virtual Workload ScheduleAsync(THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, action, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload ScheduleAsync(THandle handle, Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDI(action, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, state, action, CancellationToken.None);

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, state, action, null, cancellationToken);

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithDIAndState<TState>(state, action, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, func, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDI<TResult>(func, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, state, func, null, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithDIAndState<TState, TResult>(state, func, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }
}
