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
    public static void UseImplementation<TImpl>() where TImpl : IMemoryManager, new()
    {
        Allocator = new TImpl();
        Free = &TImpl.Free;
        Malloc = &TImpl.Malloc;
        Realloc = &TImpl.Realloc;
        Calloc = &TImpl.Calloc;
    }

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

    /// <summary>
    /// Attempts to register an external allocation with the current memory manager if allocation tracking is supported.
    /// </summary>
    /// <param name="ptr">The base address of the allocation.</param>
    /// <param name="size">The size of the allocation, in bytes.</param>
    /// <returns><see langword="true"/> if the allocation was registered; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>
    /// Attempts to unregister an external allocation with the current memory manager if allocation tracking is supported.
    /// </summary>
    /// <param name="ptr">The base address of the allocation.</param>
    /// <returns><see langword="true"/> if the allocation was unregistered; otherwise, <see langword="false"/>.</returns>
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