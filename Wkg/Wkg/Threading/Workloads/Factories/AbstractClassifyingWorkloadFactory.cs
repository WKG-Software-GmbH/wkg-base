using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Pooling;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClassifyingWorkloadFactory<THandle> : AbstractClassfulWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClassifyingWorkloadFactory(IClassifyingQdisc<THandle> root, AnonymousWorkloadPool? pool, WorkloadContextOptions? options) 
        : base(root, pool, options)
    {
    }

    private protected IClassifyingQdisc<THandle> ClassifyingRoot => Unsafe.As<IClasslessQdisc<THandle>, IClassifyingQdisc<THandle>>(ref RootRef);

    public virtual void Classify<TState>(TState state, Action action) where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        AnonymousWorkload workload = SupportsPooling
            ? Pool.Rent(action)
            : new AnonymousWorkload(action);
        ClassifyCore(state, workload);
    }

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, WorkloadContextOptions? options = null) 
        where TState : class =>
        ClassifyAsync(state, action, options, CancellationToken.None);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, CancellationToken cancellationToken) 
        where TState : class =>
        ClassifyAsync(state, action, null, cancellationToken);

    public virtual Workload ClassifyAsync<TState>(TState state, Action<CancellationFlag> action, WorkloadContextOptions? options, CancellationToken cancellationToken) 
        where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload workload = new(action, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options = null) 
        where TState : class =>
        ClassifyAsync(state, func, options, CancellationToken.None);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, CancellationToken cancellationToken) 
        where TState : class =>
        ClassifyAsync(state, func, null, cancellationToken);

    public virtual Workload<TResult> ClassifyAsync<TState, TResult>(TState state, Func<CancellationFlag, TResult> func, WorkloadContextOptions? options, CancellationToken cancellationToken) 
        where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        options ??= DefaultOptions;
        Workload<TResult> workload = new(func, options, cancellationToken);
        ClassifyCore(state, workload);
        return workload;
    }

    private protected virtual void ClassifyCore<TState>(TState state, AbstractWorkloadBase workload) where TState : class
    {
        if (!ClassifyingRoot.TryEnqueue(state, workload) && !ClassifyingRoot.TryEnqueueDirect(state, workload))
        {
            throw new WorkloadSchedulingException("The workload could not be classified.");
        }
    }
}
