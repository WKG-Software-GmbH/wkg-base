using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Unmanaged.MemoryManagement.Implementations;

/// <summary>
/// Represents a memory manager that uses the <see cref="NativeMemory"/> wrapper around libc to allocate and free unmanaged memory.
/// </summary>
public readonly unsafe struct NativeMemoryManager : IMemoryManager
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <remarks>
    /// This property always returns <see langword="false"/>.
    /// </remarks>
    public bool SupportsAllocationTracking => false;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Calloc(int count, int size) =>
        NativeMemory.AllocZeroed((nuint)(count * size));

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(void* memory) =>
        NativeMemory.Free(memory);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Malloc(int size) =>
        NativeMemory.Alloc((nuint)size);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Realloc(void* previous, int newSize) =>
        NativeMemory.Realloc(previous, (nuint)newSize);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Calloc<T>(int count) where T : unmanaged =>
        (T*)NativeMemory.AllocZeroed((nuint)(count * sizeof(T)));

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Realloc<T>(T* previous, int newCount) where T : unmanaged =>
        (T*)NativeMemory.Realloc(previous, (nuint)(newCount * sizeof(T)));
}