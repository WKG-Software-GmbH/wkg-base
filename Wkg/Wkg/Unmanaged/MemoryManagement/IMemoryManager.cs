namespace Wkg.Unmanaged.MemoryManagement;

/// <summary>
/// Represents an impmentation of a memory manager that can be used to allocate and free unamaned memory.
/// </summary>
public unsafe interface IMemoryManager
{
    /// <summary>
    /// Frees a block of previously allocated memory.
    /// </summary>
    /// <remarks>
    /// The behavior is undefined if the memory was not allocated by this memory manager.
    /// </remarks>
    /// <param name="memory">A pointer to the memory to free.</param>
    static abstract void Free(void* memory);

    /// <summary>
    /// Allocates a block of memory of the specified size.
    /// </summary>
    /// <param name="size">The size in bytes of the memory to allocate.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    static abstract void* Malloc(int size);

    /// <summary>
    /// Allocates a block of memory of the specified size and intializes it to zero.
    /// </summary>
    /// <param name="count">The number of elements to allocate.</param>
    /// <param name="size">The size in bytes of each element.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    static abstract void* Calloc(int count, int size);

    /// <summary>
    /// Reallocates a block of memory to the specified size.
    /// </summary>
    /// <remarks>
    /// The behavior is undefined if the memory was not allocated by this memory manager.
    /// </remarks>
    /// <param name="previous">A pointer to the previously allocated memory.</param>
    /// <param name="newSize">The new size in bytes of the memory block.</param>
    /// <returns>A pointer to the reallocated memory block.</returns>
    static abstract void* Realloc(void* previous, int newSize);

    /// <summary>
    /// Allocates a block of memory of the specified size and intializes it to zero.
    /// </summary>
    /// <typeparam name="T">The type of the elements to allocate.</typeparam>
    /// <param name="count">The number of elements to allocate.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    T* Calloc<T>(int count) where T : unmanaged;

    /// <summary>
    /// Reallocates a block of memory to the specified size.
    /// </summary>
    /// <typeparam name="T">The type of the elements to reallocate.</typeparam>
    /// <param name="previous">A pointer to the previously allocated memory.</param>
    /// <param name="newCount">The new number of elements to allocate.</param>
    /// <returns>A pointer to the reallocated memory block.</returns>
    T* Realloc<T>(T* previous, int newCount) where T : unmanaged;

    /// <summary>
    /// Indicates whether this memory manager supports allocation tracking for debugging purposes.
    /// </summary>
    bool SupportsAllocationTracking { get; }
}