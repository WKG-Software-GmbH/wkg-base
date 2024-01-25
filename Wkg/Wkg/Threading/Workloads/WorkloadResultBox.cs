namespace Wkg.Threading.Workloads;

internal class WorkloadResultBox<TResult>(TResult result)
{
    public TResult Result { get; } = result;
}