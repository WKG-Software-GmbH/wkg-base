using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Unmanaged.MemoryManagement.Implementations;

public readonly unsafe struct NativeMemoryManager : IMemoryManager
{
    public bool SupportsAllocationTracking => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Calloc(int count, int size) =>
        NativeMemory.AllocZeroed((nuint)(count * size));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(void* memory) =>
        NativeMemory.Free(memory);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Malloc(int size) =>
        NativeMemory.Alloc((nuint)size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Realloc(void* previous, int newSize) =>
        NativeMemory.Realloc(previous, (nuint)newSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Calloc<T>(int count) where T : unmanaged =>
        (T*)NativeMemory.AllocZeroed((nuint)(count * sizeof(T)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T* Realloc<T>(T* previous, int newCount) where T : unmanaged =>
        (T*)NativeMemory.Realloc(previous, (nuint)(newCount * sizeof(T)));
}