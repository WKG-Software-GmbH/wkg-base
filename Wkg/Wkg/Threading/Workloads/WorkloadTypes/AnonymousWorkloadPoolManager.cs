using Wkg.Data.Pooling;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadPoolManager(int _capacity)
{
    private readonly object _lock = new();
    private ObjectPool<AnonymousWorkloadImpl>? _pool;
    private ObjectPool<AnonymousWorkloadImplWithDI>? _poolWithDI;

    private ObjectPool<AnonymousWorkloadImpl> Pool
    {
        get
        {
            ObjectPool<AnonymousWorkloadImpl>? pool = Volatile.Read(ref _pool);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _pool);
                    if (pool is null)
                    {
                        DebugLog.WriteDiagnostic("Creating new anonymous workload pool.", LogWriter.Blocking);
                        pool = new ObjectPool<AnonymousWorkloadImpl>(_capacity);
                        Volatile.Write(ref _pool, pool);
                    }
                }
            }
            return pool;
        }
    }

    private ObjectPool<AnonymousWorkloadImplWithDI> PoolWithDI
    {
        get
        {
            ObjectPool<AnonymousWorkloadImplWithDI>? pool = Volatile.Read(ref _poolWithDI);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _poolWithDI);
                    if (pool is null)
                    {
                        DebugLog.WriteDiagnostic("Creating new anonymous workload pool with DI.", LogWriter.Blocking);
                        pool = new ObjectPool<AnonymousWorkloadImplWithDI>(_capacity);
                        Volatile.Write(ref _poolWithDI, pool);
                    }
                }
            }
            return pool;
        }
    }

    public AnonymousWorkloadImpl Rent(Action action)
    {
        AnonymousWorkloadImpl workload = Pool.Rent();
        workload.Initialize(action);
        return workload;
    }

    public AnonymousWorkloadImplWithDI Rent(Action<IWorkloadServiceProvider> action)
    {
        AnonymousWorkloadImplWithDI workload = PoolWithDI.Rent();
        workload.Initialize(action);
        return workload;
    }
}
