namespace Wkg.Unmanaged.MemoryManagement.Implementations.AllocationTracking;

/// <summary>
/// Represents an <see cref="IMemoryManager"/> capable of tracking allocations.
/// </summary>
public unsafe interface IAllocationTracker : IMemoryManager
{
    /// <summary>
    /// Resets the allocation tracker.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a snapshot of the current allocations.
    /// </summary>
    /// <param name="reset">Whether to reset the allocations after taking the snapshot.</param>
    /// <returns>A <see cref="AllocationSnapshot"/> of the current allocations.</returns>
    AllocationSnapshot GetAllocationSnapshot(bool reset);

    /// <summary>
    /// Registers a manual external allocation to be tracked.
    /// </summary>
    /// <param name="handle">A handle to the existing allocation.</param>
    /// <param name="size">The size of the existing allocation.</param>
    void RegisterExternalAllocation(void* handle, nuint size);

    /// <summary>
    /// Unregisters a manual external allocation from being tracked without freeing the underlying memory.
    /// </summary>
    void UnregisterExternalAllocation(void* handle);
}