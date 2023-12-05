using System.Diagnostics;

namespace Wkg.Threading.Workloads.Continuations;

internal class WorkloadContinueWithContination<TWorkload, TResult>(Action<WorkloadResult<TResult>> _continuation) : TypedWorkloadContinuation<TWorkload>
        where TWorkload : AwaitableWorkload, IWorkload<TResult>
{
    protected override void InvokeInternal(TWorkload workload)
    {
        Debug.Assert(workload.IsCompleted, "Workload must be completed at this point.");
        _continuation(workload.GetResultUnsafe());
    }
}