using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

/// <summary>
/// Represents a <see cref="ThreadLocal{T}"/> <see cref="IMemoryManager"/> capable of tracking allocations.
/// </summary>
/// <remarks>
/// Allocations are tracked per thread and are as such only visible to the thread that allocated them.
/// </remarks>
/// <typeparam name="TMemoryManager">The underlying <see cref="IMemoryManager"/> to use.</typeparam>
[RequiresUnreferencedCode("Requires reflective access to calling methods.")]
public unsafe class ThreadLocalAllocationTracker<TMemoryManager> : IMemoryManager, IAllocationTracker where TMemoryManager : struct, IMemoryManager
{
    private static readonly ThreadLocal<ConcurrentDictionary<nuint, Allocation>> s_allocations = new(() => new());

    private readonly TMemoryManager _impl = new();

    /// <summary>
    /// Creates a new instance of the <see cref="ThreadLocalAllocationTracker{TMemoryManager}"/> class.
    /// </summary>
    public ThreadLocalAllocationTracker()
    {
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <remarks>
    /// This property is always <see langword="true"/>.
    /// </remarks>
    public bool SupportsAllocationTracking => true;

    /// <inheritdoc/>
    public void Clear() => s_allocations.Value!.Clear();

    /// <inheritdoc/>
    [StackTraceHidden]
    public static void* Calloc(int count, int size)
    {
        void* p = TMemoryManager.Calloc(count, size);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)size, new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    public static void Free(void* memory)
    {
        TMemoryManager.Free(memory);
        s_allocations.Value!.TryRemove((nuint)memory, out _);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public static void* Malloc(int size)
    {
        void* p = TMemoryManager.Malloc(size);
        Allocation allocation = new(new IntPtr(p), (ulong)size, new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public static void* Realloc(void* previous, int newSize)
    {
        s_allocations.Value!.TryRemove((nuint)previous, out _);
        void* p = TMemoryManager.Realloc(previous, newSize);
        Allocation allocation = new(new IntPtr(p), (ulong)newSize, new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public T* Calloc<T>(int count) where T : unmanaged
    {
        T* p = _impl.Calloc<T>(count);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)sizeof(T), new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    public AllocationSnapshot GetAllocationSnapshot(bool reset = false)
    {
        Allocation[] allocations = [.. s_allocations.Value!.Values];
        if (reset)
        {
            s_allocations.Value!.Clear();
        }

        return new AllocationSnapshot(allocations);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public T* Realloc<T>(T* previous, int newCount) where T : unmanaged
    {
        s_allocations.Value!.TryRemove((nuint)previous, out _);
        T* p = _impl.Realloc(previous, newCount);
        Allocation allocation = new(new IntPtr(p), (ulong)newCount * (ulong)sizeof(T), new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public void RegisterExternalAllocation(void* handle, nuint size)
    {
        Allocation allocation = new(new IntPtr(handle), size, new StackTrace(fNeedFileInfo: true));
        s_allocations.Value!.TryAdd((nuint)handle, allocation);
    }

    /// <inheritdoc/>
    public void UnregisterExternalAllocation(void* handle) => s_allocations.Value!.TryRemove(new UIntPtr(handle), out _);
}