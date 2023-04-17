using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wkg.Unmanaged.MemoryManagement.Implementations;

public readonly struct MarshalMemoryManager : IMemoryManager
{
    public bool SupportsAllocationTracking => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Calloc(int count, int size)
    {
        int byteSize = count * size;
        void* ptr = Marshal.AllocHGlobal(byteSize).ToPointer();
        MemoryManager.ZeroMemory(ptr, (uint)byteSize);
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Free(void* memory) =>
        Marshal.FreeHGlobal(new IntPtr(memory));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Malloc(int size) =>
        Marshal.AllocHGlobal(size).ToPointer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void* Realloc(void* previous, int newSize) =>
        Marshal.ReAllocHGlobal(new IntPtr(previous), (IntPtr)newSize).ToPointer();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* Calloc<T>(int count) where T : unmanaged
    {
        int byteSize = count * sizeof(T);
        T* ptr = (T*)Marshal.AllocHGlobal(byteSize);
        MemoryManager.ZeroMemory(ptr, (uint)byteSize);
        return ptr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe T* Realloc<T>(T* previous, int newCount) where T : unmanaged =>
        (T*)Marshal.ReAllocHGlobal(new IntPtr(previous), (IntPtr)newCount);
}