namespace Wkg.Threading.Workloads.Continuations;

internal readonly struct WorkloadContinuationAction(Action _continuation) : IWorkloadContinuation
{
    public void Invoke(AbstractWorkloadBase workload) => _continuation();

    public void InvokeInline(AbstractWorkloadBase workload) => _continuation();
}
