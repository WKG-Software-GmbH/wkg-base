using System.Runtime.CompilerServices;

namespace Wkg.Collections.Concurrent.BitmapInternals;

internal static class ConcurrentBitmap56Extensions
{
    // each cluster must track the fullness and emptiness of its segments, so 2 bits are required per segment
    // bits 0 to 27 are used for the segment emptiness state,
    // bits 28 to 55 are used for the segment fullness state

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreChildrenEmpty(this ConcurrentBitmap56 bmp, int numberOfChildren)
    {
        // we are only interested in the lower numberOfChildren bits
        ulong mask = (1ul << numberOfChildren) - 1;
        return (bmp.GetRawData() & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreChildrenFull(this ConcurrentBitmap56 bmp, int numberOfChildren)
    {
        // we are only interested in the upper 28 bits
        ulong mask = ((1ul << numberOfChildren) - 1) << 28;
        return (bmp.GetRawData() & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChildEmpty(this ConcurrentBitmap56 bmp, int childIndex) =>
        // read the childIndex-th bit in the lower 28 bits
        (bmp.GetRawData() & (1ul << childIndex)) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChildFull(this ConcurrentBitmap56 bmp, int childIndex) =>
        // read the childIndex-th bit in the upper 28 bits
        (bmp.GetRawData() & (1ul << (28 + childIndex))) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 SetChildEmpty(this ConcurrentBitmap56 bmp, int childIndex)
    {
        // write 1 to the childIndex-th bit in the lower 28 bits and 0 to the childIndex-th bit in the upper 28 bits
        ulong mask = 1ul << childIndex;
        // important: we need to preserve the full state (including the guard token).
        return new ConcurrentBitmap56((bmp.GetFullState() | mask) & ~(mask << 28));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 SetChildFull(this ConcurrentBitmap56 bmp, int childIndex)
    {
        // write 1 to the childIndex-th bit in the upper 28 bits and 0 to the childIndex-th bit in the lower 28 bits
        ulong mask = 1ul << childIndex;
        // important: we need to preserve the full state (including the guard token).
        return new ConcurrentBitmap56((bmp.GetFullState() & ~mask) | (mask << 28));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 ClearChildEmpty(this ConcurrentBitmap56 bmp, int childIndex)
    {
        // write 0 to the childIndex-th bit in the lower 28 bits
        ulong mask = 1ul << childIndex;
        // important: we need to preserve the full state (including the guard token).
        return new ConcurrentBitmap56(bmp.GetFullState() & ~mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ConcurrentBitmap56 ClearChildFull(this ConcurrentBitmap56 bmp, int childIndex)
    {
        // write 0 to the childIndex-th bit in the upper 28 bits
        ulong mask = 1ul << childIndex;
        // important: we need to preserve the full state (including the guard token).
        return new ConcurrentBitmap56(bmp.GetFullState() & ~(mask << 28));
    }
}