using System.Collections.Concurrent;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading;

namespace Wkg.Data.Pooling;

/// <summary>
/// A pool that uses a <see cref="ConcurrentBag{T}"/> to store the pooled items.
/// </summary>
/// <typeparam name="T">The type of the pooled items.</typeparam>
public class StrongPool<T> : IPool<T> where T : class, IPoolable<T>
{
    private readonly ConcurrentBag<T> _pool;
    private readonly int _maxCapacity;
    private int _capacity;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrongPool{T}"/> class.
    /// </summary>
    /// <param name="maxCapacity">The maximum capacity of the pool.</param>
    public StrongPool(int maxCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCapacity, nameof(maxCapacity));
        _pool = [];
        _maxCapacity = maxCapacity;
        _capacity = 0;
    }

    /// <inheritdoc/>
    public int MaxCapacity => _maxCapacity;

    /// <inheritdoc/>
    public int Count => Volatile.Read(ref _capacity);

    /// <inheritdoc/>
    public T Rent()
    {
        DebugLog.WriteDiagnostic($"Renting a {typeof(T).Name} from the {nameof(StrongPool<T>)}.", LogWriter.Blocking);
        if (Atomic.DecrementClampMinFast(ref _capacity, 0) > 0 && _pool.TryTake(out T? item))
        {
            return item;
        }
        return T.Create(this);
    }

    /// <inheritdoc/>
    public bool Return(T item)
    {
        DebugLog.WriteDiagnostic($"Returning a {typeof(T).Name} to the {nameof(WeakPool<T>)}.", LogWriter.Blocking);
        
        if (Atomic.IncrementClampMaxFast(ref _capacity, _maxCapacity) < _maxCapacity)
        {
            _pool.Add(item);
            return true;
        }
        return false;
    }
}
