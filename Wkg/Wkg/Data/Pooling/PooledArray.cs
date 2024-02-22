﻿using System.Diagnostics;
using Wkg.Common.ThrowHelpers;

namespace Wkg.Data.Pooling;

/// <summary>
/// Represents an array that has been rented from an <see cref="System.Buffers.ArrayPool{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the elements in the array.</typeparam>"
public readonly struct PooledArray<T>
{
    private readonly T[] _array;
    private readonly int _length;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledArray{T}"/> struct with the specified array and usage length.
    /// </summary>
    /// <param name="array">The array to wrap.</param>
    /// <param name="actualLength">The usable length of the array that contains valid data.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="actualLength"/> is negative or greater than the length of the array.</exception>
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

    /// <summary>
    /// Gets the length of the usable portion of the array.
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// Gets the underlying array.
    /// </summary>
    public T[] Array => _array;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <remarks>
    /// The specified <paramref name="index"/> must be within the bounds of the usable portion of the array.
    /// </remarks>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is negative or greater than or equal to <see cref="Length"/>.</exception>
    public ref T this[int index]
    {
        get
        {
            Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _length - 1, nameof(index));
            return ref _array[index];
        }
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> that represents the usable portion of the array.
    /// </summary>
    public Span<T> AsSpan() => _array.AsSpan(0, _length);

    /// <summary>
    /// Attempts to resize the usable portion of the array to the specified length.
    /// The operation fails if the specified length is greater than the size of the underlying array.
    /// </summary>
    /// <param name="newLength">The new length of the usable portion of the array.</param>
    /// <param name="resized">The resized array if the operation was successful; otherwise, the original array.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="newLength"/> is negative.</exception>"
    public bool TryResize(int newLength, out PooledArray<T> resized)
    {
        if (newLength > _array.Length)
        {
            resized = this;
            return false;
        }
        ArgumentOutOfRangeException.ThrowIfNegative(newLength, nameof(newLength));
        resized = new PooledArray<T>(_array, newLength, noChecks: true);
        return true;
    }

    /// <summary>
    /// Attempts to resize the usable portion of the array to the specified length.
    /// The operation fails if the specified length is greater than the size of the underlying array.
    /// </summary>
    /// <remarks>
    /// This method does not perform any parameter validation and assumes that the specified length is not negative.
    /// </remarks>
    /// <param name="newLength">The new length of the usable portion of the array.</param>
    /// <param name="resized">The resized array if the operation was successful; otherwise, the original array.</param>
    /// <returns><see langword="true"/> if the operation was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryResizeUnsafe(int newLength, out PooledArray<T> resized)
    {
        if (newLength > _array.Length)
        {
            resized = this;
            return false;
        }
        resized = new PooledArray<T>(_array, newLength, noChecks: true);
        return true;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the usable portion of the array.
    /// </summary>
    public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}
