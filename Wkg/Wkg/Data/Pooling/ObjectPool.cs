using System.Diagnostics;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading;

namespace Wkg.Data.Pooling;

/// <summary>
/// A pool that attempts to reuse objects, allocating new ones only when the pool is empty.
/// </summary>
/// <remarks>
/// This pool is thread safe and prioritizes returning over renting.
/// </remarks>
/// <typeparam name="T">The type of the object to pool.</typeparam>
public class ObjectPool<T> : IPool<T>, IDisposable where T : class, IPoolable<T>
{
    private readonly T?[] _pool;

    // Renting is thread safe with any concurrent number of renters, and returning is also thread safe with any number of concurrent returners,
    // but interleaving of renting and returning is a serios issue. As such, we use an alpha-beta lock to ensure that only one type of the two operations
    // can be performed at a time, possibly with multiple concurrent operations of the same type.
    // returning takes precedence over renting, so we use the beta lock for renting and the alpha lock for returning.
    // this will ensure that elements are reused as much as possible.
    private readonly AlphaBetaLockSlim _abls;

    /// <summary>
    /// Points to the the next free index in the array.
    /// </summary>
    private int _index;
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectPool{T}"/> class.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of objects to pool.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxCapacity"/> is less than or equal to zero.</exception>
    public ObjectPool(int maxCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCapacity, nameof(maxCapacity));
        _pool = new T[maxCapacity];
        _index = 0;
        _abls = new AlphaBetaLockSlim();
    }

    /// <inheritdoc/>
    public int MaxCapacity => _pool.Length;

    /// <inheritdoc/>
    public int Count => Volatile.Read(ref _index);

    /// <inheritdoc/>
    public T Rent()
    {
        // no need to check for disposed, as the ABLS will throw an ObjectDisposedException if it is disposed
        DebugLog.WriteDiagnostic($"Renting a {typeof(T).Name} from the {nameof(ObjectPool<T>)}.", LogWriter.Blocking);
        // returning takes precedence over renting, so we use the beta lock here
        using ILockOwnership betaLock = _abls.AcquireBetaLock();
        int original = Atomic.DecrementClampMinFast(ref _index, 0);
        int myIndex = original - 1;
        if (myIndex < 0)
        {
            return T.Create(this);
        }
        T element = Interlocked.Exchange(ref _pool[myIndex], null)!;
        Debug.Assert(element is not null, "The pool should never contain null elements on a non-empty index.");
        return element;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method does not guarantee that the object will be successfully returned to the pool, even if the pool is not full.
    /// </remarks>
    public bool Return(T item)
    {
        // no need to check for disposed, as the ABLS will throw an ObjectDisposedException if it is disposed
        ArgumentNullException.ThrowIfNull(item, nameof(item));

        DebugLog.WriteDiagnostic($"Returning a {typeof(T).Name} to the {nameof(ObjectPool<T>)}.", LogWriter.Blocking);
        // acquire the alpha lock to ensure that no other thread is renting while we (and possibly other threads) are returning
        using ILockOwnership alphaLock = _abls.AcquireAlphaLock();
        int myIndex = Atomic.IncrementClampMaxFast(ref _index, _pool.Length - 1);
        if (myIndex + 1 < _pool.Length)
        {
            T? old = Interlocked.CompareExchange(ref _pool[myIndex], item, null);
            Debug.Assert(old is null, "The pool should never contain null elements on a non-empty index.");
            return true;
        }
        return false;
    }

    private void Dispose(bool disposing)
    {
        if (disposing && !_disposedValue)
        {
            foreach (T? item in _pool)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _abls.Dispose();
            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}