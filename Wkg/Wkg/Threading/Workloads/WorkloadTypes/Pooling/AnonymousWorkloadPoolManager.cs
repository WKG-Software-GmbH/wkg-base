﻿using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes.Pooling;

internal class AnonymousWorkloadPoolManager
{
    private readonly int _capacity;
    private readonly object _lock = new();
    private AnonymousWorkloadPool<AnonymousWorkloadImpl>? _pool;
    private AnonymousWorkloadPool<AnonymousWorkloadImplWithDI>? _poolWithDI;

    public AnonymousWorkloadPoolManager(int capacity)
    {
        _capacity = capacity;
    }

    private AnonymousWorkloadPool<AnonymousWorkloadImpl> Pool
    {
        get
        {
            AnonymousWorkloadPool<AnonymousWorkloadImpl>? pool = Volatile.Read(ref _pool);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _pool);
                    if (pool is null)
                    {
                        pool = new AnonymousWorkloadPool<AnonymousWorkloadImpl>(_capacity);
                        Volatile.Write(ref _pool, pool);
                    }
                }
            }
            return pool;
        }
    }

    private AnonymousWorkloadPool<AnonymousWorkloadImplWithDI> PoolWithDI
    {
        get
        {
            AnonymousWorkloadPool<AnonymousWorkloadImplWithDI>? pool = Volatile.Read(ref _poolWithDI);
            if (pool is null)
            {
                lock (_lock)
                {
                    pool = Volatile.Read(ref _poolWithDI);
                    if (pool is null)
                    {
                        pool = new AnonymousWorkloadPool<AnonymousWorkloadImplWithDI>(_capacity);
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
