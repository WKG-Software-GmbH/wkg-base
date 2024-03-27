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
    private static readonly ThreadLocal<ConcurrentDictionary<nuint, Allocation>> _allocations = new(() => new());

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
    public void Clear() => _allocations.Value!.Clear();

    /// <inheritdoc/>
    [StackTraceHidden]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static void* Calloc(int count, int size)
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        void* p = TMemoryManager.Calloc(count, size);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)size, new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static void Free(void* memory)
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        TMemoryManager.Free(memory);
        _allocations.Value!.TryRemove((nuint)memory, out _);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static void* Malloc(int size)
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        void* p = TMemoryManager.Malloc(size);
        Allocation allocation = new(new IntPtr(p), (ulong)size, new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    public static void* Realloc(void* previous, int newSize)
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        _allocations.Value!.TryRemove((nuint)previous, out _);
        void* p = TMemoryManager.Realloc(previous, newSize);
        Allocation allocation = new(new IntPtr(p), (ulong)newSize, new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public T* Calloc<T>(int count) where T : unmanaged
    {
        T* p = _impl.Calloc<T>(count);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)sizeof(T), new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    public AllocationSnapshot GetAllocationSnapshot(bool reset = false)
    {
        Allocation[] allocations = [.. _allocations.Value!.Values];
        if (reset)
        {
            _allocations.Value!.Clear();
        }

        return new AllocationSnapshot(allocations);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public T* Realloc<T>(T* previous, int newCount) where T : unmanaged
    {
        _allocations.Value!.TryRemove((nuint)previous, out _);
        T* p = _impl.Realloc(previous, newCount);
        Allocation allocation = new(new IntPtr(p), (ulong)newCount * (ulong)sizeof(T), new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    public void RegisterExternalAllocation(void* handle, nuint size)
    {
        Allocation allocation = new(new IntPtr(handle), size, new StackTrace(fNeedFileInfo: true));
        _allocations.Value!.TryAdd((nuint)handle, allocation);
    }

    /// <inheritdoc/>
    public void UnregisterExternalAllocation(void* handle) => _allocations.Value!.TryRemove(new UIntPtr(handle), out _);
}