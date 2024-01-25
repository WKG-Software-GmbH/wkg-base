namespace Wkg.Threading.Workloads;

public interface IWorkload
{
    internal WorkloadResult GetResultUnsafe();
}

public interface IWorkload<TResult>
{
    internal WorkloadResult<TResult> GetResultUnsafe();
}