namespace Wkg.Unmanaged.MemoryManagement;

public unsafe interface IMemoryManager
{
    static abstract void Free(void* memory);

    static abstract void* Malloc(int size);

    static abstract void* Calloc(int count, int size);

    static abstract void* Realloc(void* previous, int newSize);

    T* Calloc<T>(int count) where T : unmanaged;

    T* Realloc<T>(T* previous, int newCount) where T : unmanaged;

    bool SupportsAllocationTracking { get; }
}