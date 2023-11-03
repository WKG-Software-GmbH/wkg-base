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

    // we know that the root is classful, so we can safely do this
    private protected IClassfulQdisc<THandle> ClassfulRoot => Unsafe.As<IClasslessQdisc<THandle>, IClassfulQdisc<THandle>>(ref RootRef);

    public virtual void Classify<TState>(TState state, Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImpl(action);
        ClassifyCore(state, workload);
    }

    public virtual void Classify<TState>(TState state, Action<TState> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithState<TState>(state, action);
        ClassifyCore(state, workload);
    }

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, action, options, CancellationToken.None);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, CancellationToken cancellationToken) =>
        ClassifyAsync(state, action, null, cancellationToken);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken) 
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImpl(action, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, action, options, CancellationToken.None);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, CancellationFlag> action, CancellationToken cancellationToken) =>
        ClassifyAsync(state, action, null, cancellationToken);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithState<TState>(state, action, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ClassifyAsync(state, func, null, cancellationToken);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken) 
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImpl<TResult>(func, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ClassifyAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ClassifyAsync(state, func, null, cancellationToken);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithState<TState, TResult>(state, func, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual void Schedule(THandle handle, Action action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkloadImpl(action);
        ScheduleCore(handle, workload);
    }

    public virtual void Schedule<TState>(THandle handle, TState state, Action<TState> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        AnonymousWorkload workload = new AnonymousWorkloadImplWithState<TState>(state, action);
        ScheduleCore(handle, workload);
    }

    public virtual Workload ScheduleAsync(THandle handle, Action<CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, action, CancellationToken.None);

    public virtual Workload ScheduleAsync(THandle handle, Action<CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, action, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload ScheduleAsync(THandle handle, Action<CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImpl(action, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, state, action, CancellationToken.None);

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, CancellationFlag> action, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, state, action, null, cancellationToken);

    public virtual Workload ScheduleAsync<TState>(THandle handle, TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new WorkloadImplWithState<TState>(state, action, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, func, default(WorkloadContextOptions), cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TResult>(THandle handle, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImpl<TResult>(func, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options = null) =>
        ScheduleAsync(handle, state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, CancellationFlag, TResult> func, CancellationToken cancellationToken) =>
        ScheduleAsync(handle, state, func, null, cancellationToken);

    public virtual Workload<TResult> ScheduleAsync<TState, TResult>(THandle handle, TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new WorkloadImplWithState<TState, TResult>(state, func, options, cancellationToken);
        ScheduleCore(handle, workload);
        return workload;
    }

    private protected virtual void ClassifyCore<TState>(TState state, AbstractWorkloadBase workload)
    {
        if (!ClassfulRoot.TryEnqueue(state, workload) && !ClassfulRoot.TryEnqueueDirect(state, workload))
        {
            throw new WorkloadSchedulingException("The workload could not be classified.");
        }
    }

    private protected virtual void ScheduleCore(THandle handle, AbstractWorkloadBase workload)
    {
        if (_root.Handle.Equals(handle))
        {
            _root.Enqueue(workload);
        }
        else if (ClassfulRoot.TryFindChild(handle, out IClasslessQdisc<THandle>? child))
        {
            child.Enqueue(workload);
        }
        else
        {
            throw new WorkloadSchedulingException($"The workload could not be scheduled: no child qdisc with handle {handle} was found.");
        }
    }
}
