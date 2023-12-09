using System.Diagnostics;
using Wkg.Common.ThrowHelpers;

namespace Wkg.Data.Pooling;

public readonly struct PooledArray<T>
{
    private readonly T[] _array;
    private readonly int _length;

    public PooledArray(T[] array, int actualLength)
    {
        ArgumentNullException.ThrowIfNull(array, nameof(array));
        Throw.ArgumentOutOfRangeException.IfNotInRange(actualLength, 0, array.Length, nameof(actualLength));

        _array = array;
        _length = actualLength;
    }

    internal PooledArray(T[] array, int actualLength, bool noChecks)
    {
        Debug.Assert(noChecks);
        Debug.Assert(array is not null);
        Debug.Assert(actualLength >= 0 && actualLength <= array.Length);

        _array = array;
        _length = actualLength;
    }

    public int Length => _length;

    public T[] Array => _array;

    public ref T this[int index]
    {
        get
        {
            Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _length - 1, nameof(index));
            return ref _array[index];
        }
    }

    public Span<T> AsSpan() => _array.AsSpan(0, _length);

    public bool TryResize(int newLength, out PooledArray<T> resized)
    {
        if (newLength > _array.Length)
        {
            resized = default;
            return false;
        }
        ArgumentOutOfRangeException.ThrowIfNegative(newLength, nameof(newLength));
        resized = new PooledArray<T>(_array, newLength, noChecks: true);
        return true;
    }

    public bool TryResizeUnsafe(int newLength, out PooledArray<T> resized)
    {
        if (newLength > _array.Length)
        {
            resized = default;
            return false;
        }
        resized = new PooledArray<T>(_array, newLength, noChecks: true);
        return true;
    }

    public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}
