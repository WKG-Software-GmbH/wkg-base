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
        ArgumentOutOfRangeException.ThrowIfNegative(newLength, nameof(newLength));

        if (newLength > _array.Length)
        {
            resized = default;
            return false;
        }
        resized = new(_array, newLength);
        return true;
    }

    public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}
