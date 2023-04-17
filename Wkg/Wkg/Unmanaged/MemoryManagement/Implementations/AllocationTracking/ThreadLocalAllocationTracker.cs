using System.Collections.Concurrent;
using System.Diagnostics;

namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

public readonly unsafe struct ThreadLocalAllocationTracker<TMemoryManager> : IMemoryManager, IAllocationTracker where TMemoryManager : struct, IMemoryManager
{
    private static readonly ThreadLocal<ConcurrentDictionary<nuint, Allocation>> _allocations = new(() => new());

    private readonly TMemoryManager _impl = new();

    public ThreadLocalAllocationTracker()
    {
    }

    public bool SupportsAllocationTracking => true;

    public void Clear() => _allocations.Value!.Clear();

    [StackTraceHidden]
    public static void* Calloc(int count, int size)
    {
        void* p = TMemoryManager.Calloc(count, size);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)size, new StackTrace());
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    public static void Free(void* memory)
    {
        TMemoryManager.Free(memory);
        _allocations.Value!.TryRemove((nuint)memory, out _);
    }

    [StackTraceHidden]
    public static void* Malloc(int size)
    {
        void* p = TMemoryManager.Malloc(size);
        Allocation allocation = new(new IntPtr(p), (ulong)size, new StackTrace());
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    [StackTraceHidden]
    public static void* Realloc(void* previous, int newSize)
    {
        _allocations.Value!.TryRemove((nuint)previous, out _);
        void* p = TMemoryManager.Realloc(previous, newSize);
        Allocation allocation = new(new IntPtr(p), (ulong)newSize, new StackTrace());
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    [StackTraceHidden]
    public T* Calloc<T>(int count) where T : unmanaged
    {
        T* p = _impl.Calloc<T>(count);
        Allocation allocation = new(new IntPtr(p), (ulong)count * (ulong)sizeof(T), new StackTrace());
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    public AllocationSnapshot GetAllocationSnapshot(bool reset = false)
    {
        Allocation[] allocations = _allocations.Value!.Values.ToArray();
        if (reset)
        {
            _allocations.Value!.Clear();
        }

        return new AllocationSnapshot(allocations);
    }

    [StackTraceHidden]
    public T* Realloc<T>(T* previous, int newCount) where T : unmanaged
    {
        _allocations.Value!.TryRemove((nuint)previous, out _);
        T* p = _impl.Realloc(previous, newCount);
        Allocation allocation = new(new IntPtr(p), (ulong)newCount * (ulong)sizeof(T), new StackTrace());
        _allocations.Value!.TryAdd((nuint)p, allocation);
        return p;
    }

    [StackTraceHidden]
    public void RegisterExternalAllocation(void* handle, nuint size)
    {
        Allocation allocation = new(new IntPtr(handle), size, new StackTrace());
        _allocations.Value!.TryAdd((nuint)handle, allocation);
    }

    public void UnregisterExternalAllocation(void* handle) => _allocations.Value!.TryRemove(new UIntPtr(handle), out _);
}