using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads;

public class WorkloadFactory<THandle> where THandle : unmanaged
{
    private readonly IClasslessQdisc<THandle> _root;

    internal WorkloadFactory(IClasslessQdisc<THandle> root)
    {
        _root = root;
    }

    // TODO: create void overload that does not require a cancellation flag
    // we could then use an internal struct type for the workload.
    public Workload Schedule(Action<CancellationFlag> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload workload = Workload.Create(action);
        _root.Enqueue(workload);
        return workload;
    }

    public Workload Schedule<TState>(TState state, Action<CancellationFlag> action)
    {
        DebugLog.WriteDiagnostic("Scheduling new workload.", LogWriter.Blocking);
        Workload workload = Workload.Create(action);
        if (_root is IClassifyingQdisc<THandle> classifier)
        {
            if (!classifier.TryEnqueue(state, workload) && !classifier.TryEnqueueDirect(state, workload))
            {
                throw new WorkloadSchedulingException("The workload could not be classified.");
            }
        }
        else
        {
            throw new WorkloadSchedulingException("The root qdisc does not support classification.");
        }
        return workload;
    }
}
