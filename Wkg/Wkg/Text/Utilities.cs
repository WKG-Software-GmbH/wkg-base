﻿using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Wkg.Text;

/// <summary>
/// Provides helper methods for working with resource pools.
/// </summary>
internal static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int SelectBucketIndex(int bufferSize) =>
        // Buffers are bucketed so that a request between 2^(n-1) + 1 and 2^n is given a buffer of 2^n
        // Bucket index is log2(bufferSize - 1) with the exception that buffers between 1 and 16 bytes
        // are combined, and the index is slid down by 3 to compensate.
        // Zero is a valid bufferSize, and it is assigned the highest bucket index so that zero-length
        // buffers are not retained by the pool. The pool will return the Array.Empty singleton for these.
        BitOperations.Log2(((uint)bufferSize - 1) | 15) - 3;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetMaxSizeForBucket(int binIndex)
    {
        int maxSize = 16 << binIndex;
        Debug.Assert(maxSize >= 0);
        return maxSize;
    }
}