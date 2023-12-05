using System.Diagnostics;

namespace Wkg.Threading.Workloads.Continuations;

internal class WorkloadContinueWithContination<TWorkload>(Action<WorkloadResult> _continuation) : TypedWorkloadContinuation<TWorkload>
    where TWorkload : AwaitableWorkload, IWorkload
{
    protected override void InvokeInternal(TWorkload workload)
    {
        Debug.Assert(workload.IsCompleted, "Workload must be completed at this point.");
        _continuation(workload.GetResultUnsafe());
    }
}