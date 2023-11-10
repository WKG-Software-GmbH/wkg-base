using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Common.ThrowHelpers;

namespace Wkg.Unmanaged.MemoryManagement;

/// <summary>
/// Represents a wrapper around unmanaged memory.
/// </summary>
/// <remarks>
/// This type is intended to be stored in private non-readonly fields of reference types that implement the full <see cref="IDisposable"/> pattern (i.e. with a finalizer).
/// If this struct falls out of scope, you *WILL* leak memory. No not create unintentional copies of this struct, as the disposed state is not copied, and read-after-free bugs will occur.
/// </remarks>
/// <typeparam name="T">The type of the elements in the unmanaged memory.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="Unmanaged{T}"/> struct with the specified number of elements.
/// </remarks>
/// <param name="count">The number of elements to allocate.</param>
/// <param name="initialize">Whether to zero-initialize the allocated memory.</param>
public unsafe struct Unmanaged<T>(int count, bool initialize = true) : IDisposable where T : unmanaged
{
    private bool disposedValue;
    private T* _ptr = initialize
            ? (T*)MemoryManager.Calloc(count, sizeof(T))
            : (T*)MemoryManager.Malloc(count * sizeof(T));

    /// <summary>
    /// Gets the number of elements in the unmanaged memory.
    /// </summary>
    public readonly int Length { get; } = count;

    /// <summary>
    /// Gets a raw pointer to the unmanaged memory. Be sure you know what you're doing.
    /// </summary>
    public readonly T* GetPointer()
    {
        Throw.ObjectDisposedException.If(disposedValue, nameof(Unmanaged<T>));
        return _ptr;
    }

    /// <summary>
    /// Gets the element at the specified index. No bounds checking is performed, so be sure you know what you're doing.
    /// </summary>
    /// <param name="index">The index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public readonly T this[int index]
    {
        get
        {
            Debug.Assert(!disposedValue);
            Debug.Assert(index >= 0 && index < Length);
            return _ptr[index];
        }
    }

    /// <summary>
    /// Gets a span that represents the entire unmanaged memory.
    /// </summary>
    public readonly Span<T> AsSpan() => new(_ptr, Length);

    /// <summary>
    /// Gets a reference to the element at the specified index. No bounds checking is performed, so be sure you know what you're doing.
    /// </summary>
    /// <param name="index">The index of the element to get.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T GetRefUnsafe(int index)
    {
#if DEBUG
        Throw.ObjectDisposedException.If(disposedValue, nameof(Unmanaged<T>));
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));
#endif
        return ref Unsafe.AsRef<T>(_ptr + index);
    }

    /// <summary>
    /// Gets a reference to the element at the specified index in a relatively safe manner. 
    /// Bounds checking is performed, but if this struct has been copied around and another copy has been disposed, you will get a read-after-free bug.
    /// </summary>
    /// <param name="index">The index of the reference to get.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    public readonly ref T GetRef(int index)
    {
        Throw.ObjectDisposedException.If(disposedValue, nameof(Unmanaged<T>));
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));
        return ref GetRefUnsafe(index);
    }

    /// <summary>
    /// Frees the unmanaged memory.
    /// </summary>
    public void Dispose()
    {
        if (!disposedValue)
        {
            MemoryManager.Free(_ptr);
            _ptr = null;
            disposedValue = true;
        }
    }
}