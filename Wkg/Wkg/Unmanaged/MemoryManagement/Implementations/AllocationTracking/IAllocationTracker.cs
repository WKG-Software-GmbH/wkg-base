namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

public unsafe interface IAllocationTracker
{
    void Clear();

    AllocationSnapshot GetAllocationSnapshot(bool reset);

    void RegisterExternalAllocation(void* handle, nuint size);

    void UnregisterExternalAllocation(void* handle);
}