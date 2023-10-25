using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads;

public class WorkloadFactory
{
    private readonly IClasslessQdisc _root;

    internal WorkloadFactory(IClasslessQdisc root)
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
}
