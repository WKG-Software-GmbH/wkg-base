using System.Runtime.CompilerServices;
using Wkg.Unmanaged.MemoryManagement.Implementations;
using Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

namespace Wkg.Unmanaged.MemoryManagement;

/// <summary>
/// A memory manager that can be used to allocate and free memory.
/// </summary>
public static unsafe partial class MemoryManager
{
    static MemoryManager() => UseImplementation<NativeMemoryManager>();

    /// <summary>
    /// <see langword="void"/> Free(<see langword="void*"/> ptr);
    /// </summary>
    public static delegate*<void*, void> Free { get; private set; } = null;

    /// <summary>
    /// <see langword="void*"/> Malloc(<see cref="int"/> byteSize);
    /// </summary>
    public static delegate*<int, void*> Malloc { get; private set; } = null;

    /// <summary>
    /// <see langword="void*"/> Realloc(<see langword="void*"/> previous, <see cref="int"/> newByteSize);
    /// </summary>
    public static delegate*<void*, int, void*> Realloc { get; private set; } = null;

    /// <summary>
    /// <see langword="void*"/> Calloc(<see cref="int"/> elementCount, <see cref="int"/> elementSize);
    /// </summary>
    public static delegate*<int, int, void*> Calloc { get; private set; } = null;

    /// <summary>
    /// The currently used memory manager.
    /// </summary>
    public static IMemoryManager Allocator { get; private set; } = null!;

    /// <summary>
    /// Sets the memory manager to use.
    /// </summary>
    /// <typeparam name="TImpl">The type of the memory manager to use.</typeparam>
    public static void UseImplementation<TImpl>() where TImpl : struct, IMemoryManager
    {
        Allocator = new TImpl();
        Free = &TImpl.Free;
        Malloc = &TImpl.Malloc;
        Realloc = &TImpl.Realloc;
        Calloc = &TImpl.Calloc;
    }

    /// <summary>
    /// Overrides a block of memory with zeros.
    /// </summary>
    /// <typeparam name="T">The type of the elements to zero.</typeparam>
    /// <param name="ptr">The pointer to the first element to zero.</param>
    /// <param name="elementCount">The number of elements to zero.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ZeroMemory<T>(T* ptr, uint elementCount) where T : unmanaged =>
        ZeroMemory((void*)ptr, elementCount * (uint)sizeof(T));

    /// <summary>
    /// Overrides a block of memory with zeros.
    /// </summary>
    /// <param name="ptr">The base address of the block of memory to zero.</param>
    /// <param name="byteSize">The size of the block of memory to zero, in bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ZeroMemory(void* ptr, uint byteSize) =>
        Unsafe.InitBlockUnaligned(ptr, 0x0, byteSize);

    /// <summary>
    /// Copies a block of memory from one location to another.
    /// </summary>
    /// <param name="destination">The base address of the destination.</param>
    /// <param name="source">The base address of the block of memory to copy.</param>
    /// <param name="byteSize">The size of the block of memory to copy, in bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Memcpy(void* destination, void* source, uint byteSize) =>
        Unsafe.CopyBlockUnaligned(destination, source, byteSize);

    /// <summary>
    /// Sets a block of memory to a given value.
    /// </summary>
    /// <param name="ptr">The base address of the block of memory to fill.</param>
    /// <param name="value">The value to be set.</param>
    /// <param name="byteSize">The size of the block of memory to fill, in bytes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Memset(void* ptr, byte value, uint byteSize) =>
        Unsafe.InitBlockUnaligned(ptr, value, byteSize);

    /// <summary>
    /// Gets a snapshot of all allocations made by the current memory manager.
    /// </summary>
    /// <param name="reset">Whether to reset the allocation tracker after taking the snapshot.</param>
    /// <returns>A snapshot of all allocations made by the current memory manager or <see langword="null"/> if the current memory manager does not support allocation tracking.</returns>
    public static AllocationSnapshot? GetAllocationSnapshot(bool reset = false) =>
        Allocator is IAllocationTracker tracker
        ? tracker.GetAllocationSnapshot(reset)
        : null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryRegisterExternalAllocation(void* ptr, nuint size)
    {
        if (Allocator.SupportsAllocationTracking && Allocator is IAllocationTracker tracker)
        {
            tracker.RegisterExternalAllocation(ptr, size);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryUnregisterExternalAllocation(void* ptr)
    {
        if (Allocator.SupportsAllocationTracking && Allocator is IAllocationTracker tracker)
        {
            tracker.UnregisterExternalAllocation(ptr);
            return true;
        }
        return false;
    }
}