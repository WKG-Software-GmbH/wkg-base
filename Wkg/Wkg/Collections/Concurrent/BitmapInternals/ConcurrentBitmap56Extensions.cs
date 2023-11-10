using System.Runtime.CompilerServices;

namespace Wkg.Collections.Concurrent.BitmapInternals;

//Specification:
//--------------------------
// This class implements a data structure for atomic reads and writes (CAS) of bits using 64-bit unsigned integers.
// Each integer is divided into two parts: 56 bits of usable storage and an 8-bit guard token (implemented in ConcurrentBitmap56).
// The guard token is incremented with each write operation to prevent ABA/lost update issues. 
// The current design of ConcurrentBitmap56 is limited, and the goal is to expand it to support an unlimited maximum number of bits.
// To achieve this, we propose a hierarchical structure using the existing 56-bit bitmaps as building blocks.
// Multiple 56-bit bitmaps can be stored as segments within a larger data structure.
// 
// Key Features:
// - Each 56-bit bitmap is known as a *segment* and contains 56-bit usable storage and an 8-bit guard token.
// - Segments are grouped into a larger data structure known as a *cluster*.
// - Clusters track the emptiness-state of whole segments, again using a 56-bit bitmap.
// - Multiple clusters can be combined into a hierarchical *tree* structure, where intermediate levels track the emptiness-state of clusters in lower levels.
//   The actual data is stored in the leaf nodes of the tree.
// - To achieve scalability, we consider a multi-level tree structure.
//
// Example:
// - With two levels of 56-bit bitmaps, we would have 56 * 56 = 3136 bits of storage.
//
// Lock-Free Considerations:
// - Operations should preferably be lock-free and CAS-only based.
// - Increasing the depth of the tree structure may introduce more points of conflict, but it results in exponentially rarer write operations close to the root node.
//
// Tree Structure:
// - The tree is arranged in a way that each level has 56 times more nodes than the previous level.
// - Only the righ-most leaf node of the entire tree is allowed to be non-full.
// - leaf nodes are ordered from left to right in a way that allows efficient bit indexing in a binary tree-manner.

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
        ulong mask = (1ul << numberOfChildren) - 1 << 28;
        return (bmp.GetRawData() & mask) == mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChildEmpty(this ConcurrentBitmap56 bmp, int childIndex) =>
        // read the childIndex-th bit in the lower 28 bits
        (bmp.GetRawData() & 1ul << childIndex) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChildFull(this ConcurrentBitmap56 bmp, int childIndex) =>
        // read the childIndex-th bit in the upper 28 bits
        (bmp.GetRawData() & 1ul << 28 + childIndex) != 0;

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
        return new ConcurrentBitmap56(bmp.GetFullState() & ~mask | mask << 28);
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