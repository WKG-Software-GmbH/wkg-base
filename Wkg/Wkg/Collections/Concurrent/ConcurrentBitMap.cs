using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Wkg.Collections.Concurrent.BitmapInternals;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading;
using Wkg.Threading.Extensions;

namespace Wkg.Collections.Concurrent;

/// <summary>
/// Represents a thread-safe, concurrent bitmap (bit array) data structure.
/// </summary>
/// <remarks>
/// This class implements a data structure for atomic reads and writes (CAS) of bits using 64-bit unsigned integers (<see cref="ulong"/>).
/// Each integer is divided into two parts: 56 bits of usable storage and an 8-bit guard token (implemented in <see cref="ConcurrentBitmap56"/>).
/// The guard token is incremented with each write operation to prevent ABA/lost update issues. 
/// The current design of <see cref="ConcurrentBitmap56"/> is limited, and the goal is to expand it to support an unlimited maximum number of bits.
/// To achieve this, we propose a hierarchical structure using the existing 56-bit bitmaps as building blocks.
/// Multiple 56-bit bitmaps can be stored as segments within a larger data structure.
/// </remarks>
//Specification:
//--------------------------
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
public sealed class ConcurrentBitmap : IDisposable, IParentNode
{
    internal const int SEGMENT_BIT_SIZE = 56;
    // each cluster must track the fullness and emptiness of its segments, so 2 bits are required per segment
    // this means that the maximum number of segments per cluster is 28
    internal const int SEGMENTS_PER_CLUSTER = SEGMENT_BIT_SIZE / 2;
    internal const int CLUSTER_BIT_SIZE = SEGMENTS_PER_CLUSTER * SEGMENT_BIT_SIZE;
    internal const int INTERNAL_NODE_BIT_LIMIT = SEGMENTS_PER_CLUSTER * CLUSTER_BIT_SIZE;
    internal readonly ReaderWriterLockSlim _syncRoot;
    private volatile ConcurrentBitmapNode _root;
    private int _depth;
    private bool disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentBitmap"/> class.
    /// </summary>
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

    /// <summary>
    /// Retrieves a best-effort approximation of the number of bits that are set to 1.
    /// Note that this value is not guaranteed to be accurate, as the bitmap may be modified concurrently.
    /// </summary>
    public int VolatilePopCount
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.UnsafePopCount();
        }
    }

    /// <inheritdoc cref="VolatilePopCount"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This property is not thread-safe and should only be used when the bitmap is not expanded or shrunk concurrently (e.g., vie <see cref="InsertBitAt(int, bool, bool)"/>, <see cref="RemoveBitAt(int, bool)"/>, <see cref="Grow(int)"/>, or <see cref="Shrink(int)"/>)."/>
    /// </remarks>
    public int VolatilePopCountUnsafe => _root.UnsafePopCount();

    /// <summary>
    /// Retrieves a token for the specified index that can be used to guard against ABA/lost update issues.
    /// </summary>
    /// <param name="index">The index of the bit to retrieve the token for.</param>
    /// <returns>A token for the specified index.</returns>
    public byte GetToken(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.GetToken(index);
    }

    /// <inheritdoc cref="GetToken(int)"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This method does not perform any bounds checking. It is the caller's responsibility to ensure that the index is in range (0 &lt;= index &lt; <see cref="Length"/>).
    /// </remarks>
    public byte GetTokenUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.GetToken(index);
    }

    /// <summary>
    /// Retrieves a <see cref="GuardedBitInfo"/> snapshot (value + token) for the specified index that can be used to guard against ABA/lost update issues.
    /// </summary>
    /// <param name="index">The index of the bit to retrieve the <see cref="GuardedBitInfo"/> for.</param>
    /// <returns>A <see cref="GuardedBitInfo"/> snapshot for the specified index.</returns>
    public GuardedBitInfo GetBitInfo(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.GetBitInfo(index);
    }

    /// <inheritdoc cref="GetBitInfo(int)"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This method does not perform any bounds checking. It is the caller's responsibility to ensure that the index is in range (0 &lt;= index &lt; <see cref="Length"/>).
    /// </remarks>
    public GuardedBitInfo GetBitInfoUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.GetBitInfo(index);
    }

    /// <summary>
    /// The current size of the bitmap in bits.
    /// </summary>
    public int Length => _root.Length;

    /// <summary>
    /// Indicates whether all bits in the bitmap are set to 1.
    /// </summary>
    public bool IsFull
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.IsFull;
        }
    }

    /// <inheritdoc cref="IsFull"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This property is not thread-safe and should only be used when the bitmap is not expanded or shrunk concurrently (e.g., vie <see cref="InsertBitAt(int, bool, bool)"/>, <see cref="RemoveBitAt(int, bool)"/>, <see cref="Grow(int)"/>, or <see cref="Shrink(int)"/>)."/>
    /// </remarks>
    public bool IsFullUnsafe => _root.IsFull;

    /// <summary>
    /// Indicates whether all bits in the bitmap are set to 0.
    /// </summary>
    public bool IsEmpty
    {
        get
        {
            using ILockOwnership readLock = _syncRoot.AcquireReadLock();
            return _root.IsEmpty;
        }
    }

    /// <inheritdoc cref="IsEmpty"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This property is not thread-safe and should only be used when the bitmap is not expanded or shrunk concurrently (e.g., vie <see cref="InsertBitAt(int, bool, bool)"/>, <see cref="RemoveBitAt(int, bool)"/>, <see cref="Grow(int)"/>, or <see cref="Shrink(int)"/>)."/>
    /// </remarks>
    public bool IsEmptyUnsafe => _root.IsEmpty;

    /// <summary>
    /// Indicates whether the bit at the specified <paramref name="index"/> is set to 1.
    /// </summary>
    /// <param name="index">The index of the bit to check.</param>
    /// <returns><see langword="true"/> if the bit at the specified <paramref name="index"/> is set to 1; otherwise, <see langword="false"/>.</returns>
    public bool IsBitSet(int index)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        return _root.IsBitSet(index);
    }

    /// <inheritdoc cref="IsBitSet(int)"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This method does not perform any bounds checking. It is the caller's responsibility to ensure that the index is in range (0 &lt;= index &lt; <see cref="Length"/>).
    /// </remarks>
    public bool IsBitSetUnsafe(int index)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.IsBitSet(index);
    }

    /// <summary>
    /// Updates the bit at the specified <paramref name="index"/> to the specified <paramref name="value"/>.
    /// </summary>
    /// <param name="index">The index of the bit to update.</param>
    /// <param name="value">The value to set the bit to.</param>
    public void UpdateBit(int index, bool value)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode only when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireReadLock();
        _root.UpdateBit(index, value, out _);
    }

    /// <inheritdoc cref="UpdateBit(int, bool)"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This method does not perform any bounds checking. It is the caller's responsibility to ensure that the index is in range (0 &lt;= index &lt; <see cref="Length"/>).
    /// </remarks>
    public void UpdateBitUnsafe(int index, bool isSet)
    {
        Debug.Assert(index >= 0 && index < Length);
        _root.UpdateBit(index, isSet, out _);
    }

    /// <summary>
    /// Attempts to update the bit at the specified <paramref name="index"/> to the specified <paramref name="value"/> if the provided <paramref name="token"/> matches the current guard token of the segment that contains the bit.
    /// <para>
    /// The operation may fail if the segment has been modified concurrently since the <paramref name="token"/> was retrieved.
    /// </para>
    /// </summary>
    /// <param name="index">The index of the bit to update.</param>
    /// <param name="token">The guard token to use for the update.</param>
    /// <param name="value">The value to set the bit to.</param>
    /// <returns><see langword="true"/> if the update was successful; otherwise, <see langword="false"/>.</returns>
    public bool TryUpdateBit(int index, byte token, bool value)
    {
        Throw.ArgumentOutOfRangeException.IfNotInRange(index, 0, Length - 1, nameof(index));

        // sync root is only used in write mode when restructuring the tree or operating on cross-node boundaries
        using ILockOwnership readLock = _syncRoot.AcquireWriteLock();
        return _root.TryUpdateBit(index, token, value, out _);
    }

    /// <inheritdoc cref="TryUpdateBit(int, byte, bool)"/>
    /// <remarks>
    /// <see langword="WARNING"/>: This method does not perform any bounds checking. It is the caller's responsibility to ensure that the index is in range (0 &lt;= index &lt; <see cref="Length"/>).
    /// </remarks>
    public bool TryUpdateBitUnsafe(int index, byte token, bool isSet)
    {
        Debug.Assert(index >= 0 && index < Length);
        return _root.TryUpdateBit(index, token, isSet, out _);
    }

    /// <summary>
    /// Inserts a bit at the specified <paramref name="index"/> and sets it to the specified <paramref name="value"/>, 
    /// shifting all bits at and after the specified <paramref name="index"/> to the right. If <paramref name="grow"/> is <see langword="true"/>, 
    /// the bitmap will be grown by one bit such that the last bit (with the highest index) is not lost.
    /// </summary>
    /// <param name="index">The index of the bit to insert.</param>
    /// <param name="value">The value to set the bit to.</param>
    /// <param name="grow"><see langword="true"/> to grow the bitmap by the inserted bit; otherwise, if <see langword="false"/>, the last bit (with the highest index) will be discarded.</param>
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

    /// <summary>
    /// Removes the bit at the specified <paramref name="index"/>, shifting all bits after the specified <paramref name="index"/> to the left.
    /// If <paramref name="shrink"/> is <see langword="true"/>, the bitmap will be shrunk by one bit such, otherwise, the last bit (with the highest index) will be set to 0.
    /// </summary>
    /// <param name="index">The index of the bit to remove.</param>
    /// <param name="shrink"><see langword="true"/> to shrink the bitmap by the removed bit; otherwise, if <see langword="false"/>, the last bit (with the highest index) will be set to 0.</param>
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

    /// <summary>
    /// Grows the bitmap by the specified <paramref name="additionalSize"/>. 
    /// All new bits will be initialized to 0.
    /// </summary>
    /// <param name="additionalSize">The number of additional bits to add to the bitmap.</param>
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

    /// <summary>
    /// Shrinks the bitmap by the specified <paramref name="removalSize"/> bits by removing the last (highest index) bits.
    /// </summary>
    /// <param name="removalSize">The number of bits to remove from the bitmap.</param>
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

    /// <inheritdoc/>
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

    private void Dispose(bool disposing)
    {
        if (disposing && !disposedValue)
        {
            _root.Dispose();
            disposedValue = true;
        }
    }

    /// <inheritdoc/>
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