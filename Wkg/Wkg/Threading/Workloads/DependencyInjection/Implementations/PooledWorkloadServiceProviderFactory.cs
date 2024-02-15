using System.Diagnostics;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.DependencyInjection.Implementations;

/// <summary>
/// An implementation of <see cref="IWorkloadServiceProviderFactory"/> that allows <see cref="IWorkloadServiceProvider"/>
/// instances to be pooled and reused by flowing them from one worker lifecycle to another. Factory instances are always
/// owned by a single worker thread at a time, and are never accessed concurrently.
/// </summary>
public class PooledWorkloadServiceProviderFactory : WorkloadServiceProviderFactory, IWorkloadServiceProviderFactory
{
    private readonly Dictionary<Type, Func<object>> _serviceFactories = [];
    private readonly ReaderWriterLockSlim _poolLock = new(LockRecursionPolicy.NoRecursion);
    private volatile PooledWorkloadServiceProvider?[] _pool = new PooledWorkloadServiceProvider[8];

    /// <summary>
    /// Points to the the next free index in the array.
    /// </summary>
    private int _index;

    bool IWorkloadServiceProviderFactory.AddService<TService>(Func<TService> factory)
    {
        if (_serviceFactories.ContainsKey(typeof(TService)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TService), wrapper.Invoke);
        return true;
    }

    bool IWorkloadServiceProviderFactory.AddService<TInterface, TService>(Func<TService> factory)
    {
        if (_serviceFactories.ContainsKey(typeof(TInterface)))
        {
            return false;
        }
        FactoryWrapper<TService> wrapper = new(factory);
        _serviceFactories.Add(typeof(TInterface), wrapper.Invoke);
        return true;
    }

    IWorkloadServiceProvider IWorkloadServiceProviderFactory.GetInstance()
    {
        DebugLog.WriteDiagnostic("Renting a workload from the PooledWorkloadServiceProvider pool.", LogWriter.Blocking);

        int original = Atomic.DecrementClampMin(ref _index, 0);
        int myIndex = original - 1;
        if (myIndex < 0)
        {
            return new PooledWorkloadServiceProvider(this, _serviceFactories);
        }
        try
        {
            _poolLock.EnterReadLock();
            PooledWorkloadServiceProvider?[] pool = _pool;
            PooledWorkloadServiceProvider? provider = Volatile.Read(ref pool[myIndex]);
            if (provider is null)
            {
                DebugLog.WriteError("PooledWorkloadServiceProvider pool is corrupted! Got a non-negative index, but the provides at that index is null. Please report this bug.", LogWriter.Blocking);
                return new PooledWorkloadServiceProvider(this, _serviceFactories);
            }
            return provider;
        }
        finally
        {
            _poolLock.ExitReadLock();
        }
    }

    internal void ReturnInstance(PooledWorkloadServiceProvider instance)
    {
        DebugLog.WriteDiagnostic("Returning a provider to the PooledWorkloadServiceProvider pool.", LogWriter.Blocking);
        Debug.Assert(instance is not null);

        try
        {
            _poolLock.EnterWriteLock();

            PooledWorkloadServiceProvider?[] pool = _pool;
            int myIndex = Atomic.IncrementClampMax(ref _index, pool.Length - 1);
            if (myIndex + 1 < pool.Length)
            {
                // fast path
                Volatile.Write(ref pool[myIndex], instance);
            }
            else
            {
                // slow path: need to resize the array
                PooledWorkloadServiceProvider?[] newPool = new PooledWorkloadServiceProvider[pool.Length * 2];
                Array.Copy(pool, newPool, pool.Length);
                Volatile.Write(ref newPool[myIndex], instance);
                _pool = newPool;
            }
        }
        finally
        {
            _poolLock.ExitWriteLock();
        }
    }
}
