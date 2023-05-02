using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Unmanaged.MemoryManagement.Implementations;

/// <summary>
/// Represents a memory manager that uses the <see cref="Marshal"/> class to allocate and free global unmanaged memory.
/// </summary>
public readonly struct MarshalMemoryManager : IMemoryManager
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
    public static unsafe void* Calloc(int count, int size)
    {
        int byteSize = count * size;
        void* ptr = Marshal.AllocHGlobal(byteSize).ToPointer();
        MemoryManager.ZeroMemory(ptr, (uint)byteSize);
        return ptr;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(void* memory) =>
        Marshal.FreeHGlobal(new IntPtr(memory));

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Malloc(int size) =>
        Marshal.AllocHGlobal(size).ToPointer();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Realloc(void* previous, int newSize) =>
        Marshal.ReAllocHGlobal(new IntPtr(previous), (IntPtr)newSize).ToPointer();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* Calloc<T>(int count) where T : unmanaged
    {
        int byteSize = count * sizeof(T);
        T* ptr = (T*)Marshal.AllocHGlobal(byteSize);
        MemoryManager.ZeroMemory(ptr, (uint)byteSize);
        return ptr;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* Realloc<T>(T* previous, int newCount) where T : unmanaged =>
        (T*)Marshal.ReAllocHGlobal(new IntPtr(previous), (IntPtr)newCount);
}