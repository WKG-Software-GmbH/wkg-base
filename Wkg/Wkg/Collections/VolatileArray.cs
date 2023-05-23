using System.Collections;

namespace Wkg.Collections;

/// <summary>
/// An array of objects accessing its elements using <see cref="Volatile.Read{T}(ref T)"/> and <see cref="Volatile.Write{T}(ref T, T)"/>.
/// </summary>
/// <typeparam name="T">The underlying type of this array.</typeparam>
public class VolatileArray<T> : IEnumerable<T>, IEnumerator<T> where T : class
{
    private readonly T[] _values;
    private int _index = -1;

    /// <inheritdoc/>
    public T Current => this[_index];

    object IEnumerator.Current => Current;

    /// <inheritdoc cref="Array.Length"/>
    public int Length => _values.Length;

    /// <summary>
    /// Constructs a new instance of the <see cref="VolatileArray{T}"/> class with the specified length.
    /// </summary>
    /// <param name="length">The length of the new array.</param>
    public VolatileArray(int length)
    {
        _values = length == 0 ? Array.Empty<T>() : new T[length];
    }

    /// <summary>
    /// Gets or sets the element at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the element to access.</param>
    /// <returns>The element at the specified <paramref name="index"/>.</returns>
    public T this[int index]
    {
        get => Volatile.Read(ref _values[index]);
        set => Volatile.Write(ref _values[index], value);
    }

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (_index + 1 < _values.Length)
        {
            _index++;
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public void Reset() => _index = -1;

    /// <inheritdoc/>
    public void Dispose() => Pass(); 
}
