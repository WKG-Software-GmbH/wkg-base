using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Factories;

public abstract class AbstractClassifyingWorkloadFactory<THandle> : AbstractClassfulWorkloadFactory<THandle>
    where THandle : unmanaged
{
    private protected AbstractClassifyingWorkloadFactory(IClassifyingQdisc<THandle> root) : base(root)
    {
    }

    private protected IClassifyingQdisc<THandle> ClassifyingRoot => Unsafe.As<IClasslessQdisc<THandle>, IClassifyingQdisc<THandle>>(ref RootRef);

    public virtual void Classify<TState>(TState state, Action action) where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new anonymous workload.", LogWriter.Blocking);
        // TODO: pooling
        AnonymousWorkload workload = new(action);
        ClassifyCore(state, workload);
    }

    public virtual Workload Classify<TState>(TState state, Action<CancellationFlag> action) where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload workload = new(action);
        ClassifyCore(state, workload);
        return workload;
    }

    public virtual Workload<TResult> Classify<TState, TResult>(TState state, Func<CancellationFlag, TResult> func) where TState : class
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload<TResult> workload = new(func);
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
