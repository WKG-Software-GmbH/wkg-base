using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading;

namespace Wkg.Data.Pooling;

/// <summary>
/// A pool that attempts to reuse objects, but in scenarios of high contention it does not guarantee that all objects are successfully returned to the pool.
/// </summary>
/// <typeparam name="T">The type of the object to pool.</typeparam>
public class WeakPool<T> : IPool<T> where T : class, IPoolable<T>
{
    private readonly T?[] _pool;

    /// <summary>
    /// Points to the the next free index in the array.
    /// </summary>
    private int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakPool{T}"/> class.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of objects to pool.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is less than or equal to zero.</exception>
    public WeakPool(int maxCapacity)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(maxCapacity, nameof(maxCapacity));
        _pool = new T[maxCapacity];
        _index = 0;
    }

    /// <inheritdoc/>
    public int MaxCapacity => _pool.Length;

    /// <inheritdoc/>
    public int Count => Volatile.Read(ref _index);

    /// <inheritdoc/>
    public T Rent()
    {
        DebugLog.WriteDiagnostic($"Renting a {typeof(T).Name} from the {nameof(WeakPool<T>)}.", LogWriter.Blocking);
        int original = Atomic.DecrementClampMinFast(ref _index, 0);
        int myIndex = original - 1;
        if (myIndex < 0)
        {
            return T.Create(this);
        }
        T? workload = Interlocked.Exchange(ref _pool[myIndex], null);
        if (workload is null)
        {
            DebugLog.WriteWarning($"{nameof(WeakPool<T>)}: got a non-negative index ({myIndex}), but the {typeof(T).Name} at that index is null. Creating a new {typeof(T).Name} instead. This is a rare race condition and an indicator of high contention, but it can happen even in normal operation. Ensure that you don't see this message too often, otherwise use a {nameof(StrongPool<T>)} instead, reduce contention, or disable pooling.", LogWriter.Blocking);
            return T.Create(this);
        }
        return workload;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method does not guarantee that the object will be successfully returned to the pool, even if the pool is not full.
    /// </remarks>
    public bool Return(T item)
    {
        // TODO: we could opt for a less-weak implementation here by using a slim reader-writer lock.
        // Renting is thread safe with any concurrent number of renters, but returning is not.
        // technically returning is also thread safe with any number of concurrent returners,
        // but interleaving of renting and returning is a serios issue. At least with our index-based approach.
        // so maybe a reader-writer lock is a bit overkill, as we could even allow multiple writers and multiple readers
        // just not a mix of both, possibly there could be some merit to creating a custom lock for this (and similar) use cases.
        DebugLog.WriteDiagnostic($"Returning a {typeof(T).Name} to the {nameof(WeakPool<T>)}.", LogWriter.Blocking);
        // don't need to check for null because that should never happen
        // if it does, that's not too big of a deal either, as we'll just create a new workload as needed
        // we do the null checking on the caller thread in Rent() to avoid the overhead of the null check on the worker thread
        int myIndex = Atomic.IncrementClampMaxFast(ref _index, _pool.Length - 1);
        if (myIndex + 1 < _pool.Length)
        {
            return Interlocked.CompareExchange(ref _pool[myIndex], item, null) is null;
        }
        return false;
    }
}