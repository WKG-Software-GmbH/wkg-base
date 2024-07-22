using System.Runtime.CompilerServices;
using Wkg.Unmanaged.MemoryManagement;

namespace Wkg.Collections;

/// <summary>
/// IMPORTANT: This struct is intended for high-performance code only and should not be used otherwise!
/// <para></para>
/// Represents a resizable high-performance collection of elements.
/// </summary>
/// <remarks>
/// Important: Ensure not to create intentional or unintentional copies as this may lead to dangling pointers.
/// Therefore instances must be passed by reference (<see langword="ref"/>, <see langword="in"/>) or not at all.
/// <para>
/// Important: Ensure instances are properly disposed before falling out of scope.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of the elements in this <see cref="ResizableBuffer{T}"/>.</typeparam>
public unsafe struct ResizableBuffer<T> : IDisposable where T : unmanaged
{
    private readonly int _elementSize;
    private int _allocatedNativeLength;
    private T* _basePointer;
    private int _usedNativeLength;

    /// <summary>
    /// Gets the number of elements in this <see cref="ResizableBuffer{T}"/>.
    /// </summary>
    public readonly int Length => _usedNativeLength / _elementSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableBuffer{T}"/> struct.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of this <see cref="ResizableBuffer{T}"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="OutOfMemoryException"/>
    public ResizableBuffer(int initialCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialCapacity, 1, nameof(initialCapacity));

        _elementSize = sizeof(T);

        _usedNativeLength = 0;
        _allocatedNativeLength = initialCapacity * _elementSize;
        _basePointer = (T*)MemoryManager.Calloc(initialCapacity, sizeof(T));
    }

    /// <summary>
    /// Gets or sets the element at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index of the element to access.</param>
    /// <returns>The element at the specified <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"/>
    /// <exception cref="ObjectDisposedException"/>
    public T this[int index]
    {
        readonly get
        {
            ValidateIndex(index);
            return _basePointer[index];
        }
        set
        {
            ValidateIndex(index);
            _basePointer[index] = value;
        }
    }

    /// <summary>
    /// Copies the content of the specified <paramref name="span"/> to the end of this <see cref="ResizableBuffer{T}"/>.
    /// </summary>
    /// <param name="span">The span containing the elements to append to this <see cref="ResizableBuffer{T}"/>.</param>
    /// <param name="startIndex">The start index in <paramref name="span"/> at which the copying begins.</param>
    /// <param name="length">The number of elements to copy from <paramref name="span"/> starting at <paramref name="startIndex"/>.</param>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="OutOfMemoryException"/>
    public void Add(in ReadOnlySpan<T> span, int startIndex, int length) => 
        Add(span[startIndex..(startIndex + length)]);

    /// <inheritdoc cref="Add(in ReadOnlySpan{T}, int, int)"/>
    /// <param name="span">The span containing the elements to append to this <see cref="ResizableBuffer{T}"/>.</param>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="OutOfMemoryException"/>
    public void Add(in ReadOnlySpan<T> span)
    {
        // Check if this ResizableBuffer has been disposed
        ValidateState();

        // If the source buffer is empty we don't need to do anything.
        if (span.IsEmpty)
        {
            return;
        }

        // resize if needed
        if (_usedNativeLength + (span.Length * _elementSize) > _allocatedNativeLength)
        {
            // need to resize native memory.
            // re-allocate twice the current size.
            _allocatedNativeLength *= 2;
            _basePointer = (T*)MemoryManager.Realloc(_basePointer, _allocatedNativeLength);
        }

        // copy buffer to unmanaged memory
        span.CopyTo(NativeAsSpan()[Length..]);
        _usedNativeLength += span.Length * _elementSize;
    }

    /// <summary>
    /// Creates a new <see cref="Span{T}"/> over this <see cref="ResizableBuffer{T}"/>.
    /// </summary>
    /// <returns>The span representation of this <see cref="ResizableBuffer{T}"/>.</returns>
    public readonly Span<T> AsSpan()
    {
        ValidateState();

        return new Span<T>(_basePointer, Length);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_basePointer != null)
        {
            MemoryManager.Free(_basePointer);
            _basePointer = null;
        }
    }

    /// <summary>
    /// Copies the content of this <see cref="ResizableBuffer{T}"/> into a new array.
    /// </summary>
    /// <returns></returns>
    public readonly T[] ToArray() => AsSpan().ToArray();

    private readonly Span<T> NativeAsSpan() => new(_basePointer, _allocatedNativeLength);

    /// <exception cref="IndexOutOfRangeException"/>
    /// <exception cref="ObjectDisposedException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ValidateIndex(int index)
    {
        ValidateState();
        ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length, nameof(index));
    }

    /// <exception cref="ObjectDisposedException"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void ValidateState() => ObjectDisposedException.ThrowIf(_basePointer == null, nameof(ResizableBuffer<T>));
}
