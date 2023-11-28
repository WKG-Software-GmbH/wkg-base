using Wkg.Data.Pooling;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadPoolManager(int _capacity)
{
    private readonly object _lock = new();
    private WeakPool<AnonymousWorkloadImpl>? _pool;
    private WeakPool<AnonymousWorkloadImplWithDI>? _poolWithDI;

    private WeakPool<AnonymousWorkloadImpl> Pool
    {
        get
        {
            WeakPool<AnonymousWorkloadImpl>? pool = Volatile.Read(ref _pool);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _pool);
                    if (pool is null)
                    {
                        DebugLog.WriteDiagnostic("Creating new anonymous workload pool.", LogWriter.Blocking);
                        pool = new WeakPool<AnonymousWorkloadImpl>(_capacity, suppressContentionWarnings: true);
                        Volatile.Write(ref _pool, pool);
                    }
                }
            }
            return pool;
        }
    }

    private WeakPool<AnonymousWorkloadImplWithDI> PoolWithDI
    {
        get
        {
            WeakPool<AnonymousWorkloadImplWithDI>? pool = Volatile.Read(ref _poolWithDI);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _poolWithDI);
                    if (pool is null)
                    {
                        DebugLog.WriteDiagnostic("Creating new anonymous workload pool with DI.", LogWriter.Blocking);
                        pool = new WeakPool<AnonymousWorkloadImplWithDI>(_capacity, suppressContentionWarnings: true);
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
