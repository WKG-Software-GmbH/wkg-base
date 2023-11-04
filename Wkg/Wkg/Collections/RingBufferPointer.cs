using Wkg.Common;

namespace Wkg.Collections;

using static MathExtensions;

/// <summary>
/// A pointer to a position in a ring buffer.
/// </summary>
/// <typeparam name="T">The type of the elements in the ring buffer.</typeparam>
internal readonly struct RingBufferPointer<T>
{
    /// <summary>
    /// The ring buffer.
    /// </summary>
    private readonly T?[] _buffer;

    /// <summary>
    /// The position in the ring buffer.
    /// </summary>
    public readonly int Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="RingBufferPointer{T}"/> struct.
    /// </summary>
    /// <param name="buffer">The ring buffer.</param>
    /// <param name="value">The position in the ring buffer.</param>
    public RingBufferPointer(T?[] buffer, int value) => (_buffer, Value) = (buffer, value);

    /// <summary>
    /// Initializes a new instance of the <see cref="RingBufferPointer{T}"/> struct.
    /// </summary>
    /// <param name="buffer">The ring buffer.</param>
    public RingBufferPointer(T?[] buffer) : this(buffer, 0)
    {
        if (buffer.Length < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), $"{nameof(buffer)} must not be empty.");
        }
    }

    /// <summary>
    /// Implicitly converts a <see cref="RingBufferPointer{T}"/> to an <see cref="int"/>.
    /// </summary>
    /// <param name="pointer">The <see cref="RingBufferPointer{T}"/> to convert.</param>
    public static implicit operator int(RingBufferPointer<T?> pointer) => pointer.Value;

    /// <summary>
    /// Subtracts an integer from a <see cref="RingBufferPointer{T}"/>.
    /// </summary>
    /// <param name="pointer">The <see cref="RingBufferPointer{T}"/>.</param>
    /// <param name="i">The integer to subtract.</param>
    /// <returns>The result of the subtraction.</returns>
    public static RingBufferPointer<T?> operator -(RingBufferPointer<T?> pointer, int i) =>
        new(pointer._buffer, Modulo(pointer.Value - i, pointer._buffer.Length));

    /// <summary>
    /// Decrements a <see cref="RingBufferPointer{T}"/>.
    /// </summary>
    /// <param name="pointer">The <see cref="RingBufferPointer{T}"/>.</param>
    /// <returns>The decremented <see cref="RingBufferPointer{T}"/>.</returns>
    public static RingBufferPointer<T?> operator --(RingBufferPointer<T?> pointer) => pointer - 1;

    /// <summary>
    /// Checks if two <see cref="RingBufferPointer{T}"/>s are not equal.
    /// </summary>
    /// <param name="p1">The first <see cref="RingBufferPointer{T}"/>.</param>
    /// <param name="p2">The second <see cref="RingBufferPointer{T}"/>.</param>
    /// <returns>True if the <see cref="RingBufferPointer{T}"/>s are not equal, false otherwise.</returns>
    public static bool operator !=(RingBufferPointer<T?> p1, RingBufferPointer<T?> p2) => (int)p1 != p2;

    /// <summary>
    /// Adds an integer to a <see cref="RingBufferPointer{T}"/>.
    /// </summary>
    /// <param name="pointer">The <see cref="RingBufferPointer{T}"/>.</param>
    /// <param name="i">The integer to add.</param>
    /// <returns>The result of the addition.</returns>
    public static RingBufferPointer<T?> operator +(RingBufferPointer<T?> pointer, int i) =>
        new(pointer._buffer, Modulo(pointer.Value + i, pointer._buffer.Length));

    /// <summary>
    /// Increments a <see cref="RingBufferPointer{T}"/>.
    /// </summary>
    /// <param name="pointer">The <see cref="RingBufferPointer{T}"/>.</param>
    /// <returns>The incremented <see cref="RingBufferPointer{T}"/>.</returns>
    public static RingBufferPointer<T?> operator ++(RingBufferPointer<T?> pointer) => pointer + 1;

    /// <summary>
    /// Checks if two <see cref="RingBufferPointer{T}"/>s are equal.
    /// </summary>
    /// <param name="p1">The first <see cref="RingBufferPointer{T}"/>.</param>
    /// <param name="p2">The second <see cref="RingBufferPointer{T}"/>.</param>
    /// <returns>True if the <see cref="RingBufferPointer{T}"/>s are equal, false otherwise.</returns>
    public static bool operator ==(RingBufferPointer<T?> p1, RingBufferPointer<T?> p2) => (int)p1 == p2;

    /// <summary>
    /// Checks if two <see cref="RingBufferPointer{T}"/>s are equal.
    /// </summary>
    /// <param name="obj">The object to compare to.</param>
    /// <returns>True if the <see cref="RingBufferPointer{T}"/>s are equal, false otherwise.</returns>
    public override bool Equals(object? obj) => obj is RingBufferPointer<T?> pointer && Value == pointer.Value;

    /// <summary>
    /// Gets the hash code of the <see cref="RingBufferPointer{T}"/>.
    /// </summary>
    /// <returns>The hash code of the <see cref="RingBufferPointer{T}"/>.</returns>
    public override int GetHashCode() => HashCode.Combine(Value, _buffer);
}
