namespace Wkg.Threading.Workloads.WorkloadTypes.Pooling;

internal interface IPoolableAnonymousWorkload<TWorkload> where TWorkload : AnonymousWorkload, IPoolableAnonymousWorkload<TWorkload>
{
    static abstract TWorkload Create(AnonymousWorkloadPool<TWorkload> pool);
}
