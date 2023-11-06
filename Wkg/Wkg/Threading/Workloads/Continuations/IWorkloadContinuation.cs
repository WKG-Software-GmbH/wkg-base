namespace Wkg.Threading.Workloads.Continuations;

internal interface IWorkloadContinuation
{
    void Invoke(AbstractWorkloadBase workload);
}
