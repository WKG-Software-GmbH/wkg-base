using System.Text;
using Wkg.Collections.Concurrent.BitmapInternals;
using Wkg.Common.ThrowHelpers;
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
public class ConcurrentBitmap : IDisposable
{
    internal const int SEGMENT_BIT_SIZE = 56;
    // each cluster must track the fullness and emptiness of its segments, so 2 bits are required per segment
    // this means that the maximum number of segments per cluster is 28
    internal const int SEGMENTS_PER_CLUSTER = SEGMENT_BIT_SIZE / 2;
    internal const int CLUSTER_BIT_SIZE = SEGMENTS_PER_CLUSTER * SEGMENT_BIT_SIZE;
    internal const int INTERNAL_NODE_BIT_LIMIT = SEGMENTS_PER_CLUSTER * CLUSTER_BIT_SIZE;
    private readonly ConcurrentBitmapNode _root;
    private readonly int _depth;
    private readonly int _bitSize;
    internal readonly ReaderWriterLockSlim _syncRoot;
    private bool disposedValue;

    public ConcurrentBitmap(int bitSize)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(bitSize, nameof(bitSize));
        _bitSize = bitSize;
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
            _root = new ConcurrentBitmapInternalNode(0, depth, bitSize, null);
        }
        else
        {
            // we can create a cluster directly
            _root = new ConcurrentBitmapClusterNode(0, bitSize, null);
            _depth = 1;
        }
    }

    public byte GetToken(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.GetToken(index);
    }

    public int Length => _bitSize;

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

    public bool IsBitSet(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.IsBitSet(index);
    }

    public void UpdateBit(int index, bool value)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        _root.UpdateBit(index, value);
    }

    public bool TryUpdateBit(int index, byte token, bool value)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // sync root is only used in write mode when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireWriteLock();
        return _root.TryUpdateBit(index, token, value);
    }

    public void InsertBitAt(int index, bool value)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // requires global write lock
        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        _root.InsertBitAt(index, value, out bool lastBit);
        _root.RefreshState();
    }

    public void RemoveBitAt(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, _bitSize - 1, nameof(index));

        // requires global write lock
        using ILockOwnership writeLock = _syncRoot.AcquireWriteLock();
        _root.RemoveBitAt(index);
        _root.RefreshState();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder(nameof(ConcurrentBitmap))
            .Append("(Total size: ")
            .Append(_bitSize)
            .Append(" bits, depth: ")
            .Append(_depth)
            .AppendLine(")");

        _root.ToString(sb, 0);

        return sb.ToString();
    }

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
