using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wkg.Collections.Concurrent.BitmapInternals;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading;
using Wkg.Threading.Extensions;

namespace Wkg.Collections.Concurrent;

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
// - 28 (56/2) segments are grouped into a larger data structure known as a *cluster*.
//   we need to use 2 bits per segment to track whether it is full or empty, or neither, so 56/2 = 28 segments per cluster.
// - Clusters track the emptiness-state of whole segments, again using a 56-bit bitmap.
// - Multiple clusters can be combined into a hierarchical *tree* structure, where intermediate levels (internal nodes) track the emptiness-state of clusters in lower levels.
//   The actual data is stored in the leaf nodes of the tree.
//
// Example:
// - With two levels of 56-bit bitmaps, we would have 56 * 28 = 1568 bits of storage in 224 bytes.
//
// Lock-Free Considerations:
// - Except for cross-segment and cross-cluster operations, all operations are lock-free and CAS-only based.
// - Increasing the depth of the tree structure may introduce more points of conflict, but it results in exponentially rarer write operations close to the root node.
//
// Tree Structure:
// - The tree is arranged in a way that each level has 28 times more nodes than the previous level,
//   but leaf clusters have 56 times more segments than the previous level has clusters.
// - Only the righ-most segment of the entire tree is allowed to be partially full, all other segments must be full.
// - leaf nodes are ordered by index range from left to right in a way that allows efficient bit indexing in a binary tree-manner.
public class ConcurrentBitmap : IDisposable, IParentNode
{
    internal const int SEGMENT_BIT_SIZE = 56;
    // each cluster must track the fullness and emptiness of its segments, so 2 bits are required per segment
    // this means that the maximum number of segments per cluster is 28
    internal const int SEGMENTS_PER_CLUSTER = SEGMENT_BIT_SIZE / 2;
    internal const int CLUSTER_BIT_SIZE = SEGMENTS_PER_CLUSTER * SEGMENT_BIT_SIZE;
    internal const int INTERNAL_NODE_BIT_LIMIT = SEGMENTS_PER_CLUSTER * CLUSTER_BIT_SIZE;
    private volatile ConcurrentBitmapNode _root;
    private int _depth;
    internal readonly ReaderWriterLockSlim _syncRoot;
    private bool disposedValue;

    public ConcurrentBitmap(int bitSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bitSize, nameof(bitSize));
        _syncRoot = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        // do we need an internal root node, or can we just create a cluster directly?
        if (bitSize > CLUSTER_BIT_SIZE)
        {
            // what is the depth of the tree?
            int depth = 0;
            int remainingBits = bitSize;
            while (remainingBits > 0)
            {
                depth++;
                remainingBits /= CLUSTER_BIT_SIZE;
            }
            _depth = depth;

            // we need at least one internal node
            _root = new ConcurrentBitmapInternalNode(0, 0, depth, bitSize, this, null);
        }
        else
        {
            // we can create a cluster directly
            _root = new ConcurrentBitmapClusterNode(0, 0, bitSize, this);
            _depth = 1;
        }
    }

    void IParentNode.ReplaceChildNode(int index, ConcurrentBitmapNode newNode)
    {
        Debug.Assert(index == 0);
        _root = newNode;
        _depth--;
    }

    public int VolatilePopCount
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.UnsafePopCount();
        }
    }

    public int VolatilePopCountUnsafe => _root.UnsafePopCount();

    public byte GetToken(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.GetToken(index);
    }

    public byte GetTokenUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.GetToken(index);
    }

    public GuardedBitInfo GetBitInfo(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.GetBitInfo(index);
    }

    public GuardedBitInfo GetBitInfoUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.GetBitInfo(index);
    }

    public int Length => _root.Length;

    public bool IsFull
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.IsFull;
        }
    }

    public bool IsEmpty
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.IsEmpty;
        }
    }

    public bool IsEmptyUnsafe => _root.IsEmpty;

    public bool IsBitSet(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.IsBitSet(index);
    }

    public bool IsBitSetUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.IsBitSet(index);
    }

    public void UpdateBit(int index, bool isSet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        _root.UpdateBit(index, isSet, out _);
    }

    public void UpdateBitUnsafe(int index, bool isSet)
    {
        Debug.Assert(index >= 0 && index < Length);
        _root.UpdateBit(index, isSet, out _);
    }

    public bool TryUpdateBit(int index, byte token, bool isSet)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireWriteLock();
        return _root.TryUpdateBit(index, token, isSet, out _);
    }

    public bool TryUpdateBitUnsafe(int index, byte token, bool isSet)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.TryUpdateBit(index, token, isSet, out _);
    }

    public void InsertBitAt(int index, bool value, bool grow = false)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, grow ? Length : Length - 1, nameof(index));

        // requires global write lock
        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        if (grow)
        {
            GrowCore(1);
        }
        _root.InsertBitAt(index, value, out bool lastBit);
        _root.RefreshState(index);
    }

    public void RemoveBitAt(int index, bool shrink = false)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // requires global write lock
        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        _root.RemoveBitAt(index);
        if (shrink)
        {
            ShrinkCoreUnsafe(1);
        }
        _root.RefreshState(index);
    }

    public void Grow(int additionalSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(additionalSize, nameof(additionalSize));

        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        GrowCore(additionalSize);
    }

    private void GrowCore(int additionalSize)
    {
        // requires global write lock
        // we don't know if the root node can grow, so we need to check
        if (_root.Length + additionalSize <= _root.MaxNodeBitLength)
        {
            _root.Grow(additionalSize);
            return;
        }
        int totalSize = _root.Length + additionalSize;
        _root.Grow(_root.MaxNodeBitLength - _root.Length);
        int remainingSize = totalSize;
        while (remainingSize > 0)
        {
            // we need to grow the tree
            int newMaxNodeBitLength = _root.MaxNodeBitLength * SEGMENTS_PER_CLUSTER;
            int newCapactiy = Math.Min(newMaxNodeBitLength, totalSize);
            _root = new ConcurrentBitmapInternalNode(0, 0, _depth, newCapactiy, this, _root);
            remainingSize -= newCapactiy;
            _depth++;
        }
    }

    public void Shrink(int removalSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(removalSize, nameof(removalSize));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(removalSize, Length, nameof(removalSize));

        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        ShrinkCoreUnsafe(removalSize);
    }

    private void ShrinkCoreUnsafe(int removalSize) =>
        // requires global write lock
        _root.Shrink(removalSize);

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder(nameof(ConcurrentBitmap))
            .Append("(Total size: ")
            .Append(Length)
            .Append(" bits, depth: ")
            .Append(_depth)
            .AppendLine(")");

        _root.ToString(sb, 0);

        return sb.ToString();
    }

#if DEBUG
    internal string DebuggerDisplay => ToString();
#endif

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _root.Dispose();
            }
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// A struct that contains information about a bit in a <see cref="ConcurrentBitmap"/>. 
/// The validity of the returned information is protected by a guard token.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly struct GuardedBitInfo
{
    /// <summary>
    /// Whether the bit is set or not.
    /// </summary>
    // accessed very frequently, so 32 bit aligned
    [FieldOffset(0)]
    public readonly bool IsSet;
    // kinda whatever, not oftenly accessed
    [FieldOffset(2)]
    private readonly ushort IndexLow;
    /// <summary>
    /// The guard token of the segment that contains the bit.
    /// </summary>
    // also accessed very frequently, so 32 bit aligned
    [FieldOffset(4)]
    public readonly byte Token;
    // also who cares, not oftenly accessed
    [FieldOffset(6)]
    private readonly ushort IndexHigh;

    internal GuardedBitInfo(bool isSet, byte token, int index)
    {
        IsSet = isSet;
        Token = token;
        IndexLow = (ushort)index;
        IndexHigh = (ushort)(index >> 16);
    }

    /// <summary>
    /// The index of the bit.
    /// </summary>
    public int Index => (IndexHigh << 16) | IndexLow;
}