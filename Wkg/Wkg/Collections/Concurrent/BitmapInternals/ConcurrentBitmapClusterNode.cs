using System.Diagnostics;
using System.Text;
using Wkg.Common;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Unmanaged.MemoryManagement;

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

using static ConcurrentBitmap;

internal class ConcurrentBitmapClusterNode : ConcurrentBitmapNode, IDisposable
{
    // each cluster must track the fullness and emptiness of its segments, so 2 bits are required per segment
    // bits 0 to 27 are used for the segment emptiness state,
    // bits 28 to 55 are used for the segment fullness state
    private ConcurrentBitmap56State _clusterState;
    private bool disposedValue;
    // this class is very hot, as it's used by workload scheduling
    // we use an unmanaged array to avoid the overhead of the managed array
    private Unmanaged<ConcurrentBitmap56State> _segments;
    private int _usedSegmentCount;
    private int _lastSegmentSize;

    public ConcurrentBitmapClusterNode(int externalNodeIndex, int baseAddress, int clusterBitSize, IParentNode parent) : base(externalNodeIndex, baseAddress, parent, clusterBitSize)
    {
        Debug.Assert(clusterBitSize > 0);
        Debug.Assert(clusterBitSize <= CLUSTER_BIT_SIZE);
        // round up to nearest multiple of SEGMENT_BIT_SIZE
        int segmentCount = (clusterBitSize + SEGMENT_BIT_SIZE - 1) / SEGMENT_BIT_SIZE;
        _segments = new Unmanaged<ConcurrentBitmap56State>(segmentCount, initialize: true);
        _lastSegmentSize = clusterBitSize % SEGMENT_BIT_SIZE;
        _usedSegmentCount = segmentCount;
        // correct for last segment size if we have a perfect multiple of SEGMENT_BIT_SIZE
        if (_lastSegmentSize == 0)
        {
            _lastSegmentSize = SEGMENT_BIT_SIZE;
        }

        // initialize the node state
        ConcurrentBitmap56 state = default;
        for (int i = 0; i < _segments.Length; i++)
        {
            state = state.SetChildEmpty(i);
        }
        ConcurrentBitmap56.VolatileWrite(ref _clusterState, state);
    }

    internal override int NodeLength => Volatile.Read(ref _usedSegmentCount);

    public override int MaxNodeBitLength => CLUSTER_BIT_SIZE;

    public override bool IsLeaf => true;

    public override bool IsFull => ConcurrentBitmap56.VolatileRead(ref _clusterState).AreChildrenFull(_usedSegmentCount);

    public override bool IsEmpty => ConcurrentBitmap56.VolatileRead(ref _clusterState).AreChildrenEmpty(_usedSegmentCount);

    internal override ref ConcurrentBitmap56State InternalStateBitmap => ref _clusterState;

    public override byte GetToken(int index) => ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(index / SEGMENT_BIT_SIZE)).GetToken();

    public override bool IsBitSet(int index) => ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(index / SEGMENT_BIT_SIZE)).IsBitSet(index % SEGMENT_BIT_SIZE);

    internal override bool Shrink(int removalSize)
    {
        // requires global write lock
        Debug.Assert(removalSize >= 0);
        Debug.Assert(_bitSize - removalSize >= 0);

        if (removalSize == 0)
        {
            return false;
        }

        bool stateChanged = false;
        // do we need to remove segments?
        if (_lastSegmentSize - removalSize > 0)
        {
            // no, we can just shrink the last segment
            int lastSegmentIndex = _usedSegmentCount - 1;
            _lastSegmentSize -= removalSize;
            ConcurrentBitmap56 clusterState = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            // did this change anything about the emptiness state of the segment?
            if (UpdateStateSnapshot(ref clusterState, lastSegmentIndex))
            {
                // update the cluster state
                ConcurrentBitmap56.VolatileWrite(ref _clusterState, clusterState, updateToken: true);
                stateChanged = true;
            }
        }
        else
        {
            // yes, we need to remove segments
            // how many segments do we need to remove?
            int lastSegmentRemovalSize = removalSize - _lastSegmentSize;
            // we always remove at least one segment (lastSegmentRemovalSize can be 0, but we don't want to keep an empty segment)
            int removedSegmentCount = FastMath.Max((lastSegmentRemovalSize + SEGMENT_BIT_SIZE - 1) / SEGMENT_BIT_SIZE, 1);
            int newTotalSegmentCount = _usedSegmentCount - removedSegmentCount;
            _lastSegmentSize = SEGMENT_BIT_SIZE - (lastSegmentRemovalSize % SEGMENT_BIT_SIZE);
            int newLastSegmentIndex = newTotalSegmentCount - 1;
            // update the cluster state
            // we know that we removed segments, so we can just clear the bits
            // we only need special handling for the new last segment
            ConcurrentBitmap56 clusterState = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            for (int i = newLastSegmentIndex + 1; i < _usedSegmentCount; i++)
            {
                clusterState = clusterState.ClearChildEmpty(i).ClearChildFull(i);
            }
            _usedSegmentCount = newTotalSegmentCount;
            UpdateStateSnapshot(ref clusterState, newLastSegmentIndex);
            ConcurrentBitmap56.VolatileWrite(ref _clusterState, clusterState, updateToken: true);
            stateChanged = true;
        }
        // update the bit size
        _bitSize -= removalSize;
        return stateChanged;
    }

    internal override bool Grow(int additionalSize)
    {
        // requires global write lock
        Debug.Assert(additionalSize >= 0);
        Debug.Assert(_bitSize + additionalSize <= CLUSTER_BIT_SIZE);

        if (additionalSize == 0)
        {
            return false;
        }

        bool stateChanged = false;
        // do we need more segments?
        if (_lastSegmentSize + additionalSize < SEGMENT_BIT_SIZE)
        {
            // no, we can just grow the last segment
            int lastSegmentIndex = _usedSegmentCount - 1;
            _lastSegmentSize += additionalSize;
            ConcurrentBitmap56 clusterState = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            if (UpdateStateSnapshot(ref clusterState, lastSegmentIndex))
            {
                // update the cluster state
                ConcurrentBitmap56.VolatileWrite(ref _clusterState, clusterState, updateToken: true);
                stateChanged = true;
            }
        }
        else
        {
            // yes, we need to add more segments
            // how many segments do we need to add?
            int lastSegmentIndex = _usedSegmentCount - 1;
            int lastSegmentAdditionalSize = SEGMENT_BIT_SIZE - _lastSegmentSize;
            int additionalSizeForNewSegments = additionalSize - lastSegmentAdditionalSize;
            int additionalSegmentCount = (additionalSizeForNewSegments + SEGMENT_BIT_SIZE - 1) / SEGMENT_BIT_SIZE;
            int newTotalSegmentCount = _usedSegmentCount + additionalSegmentCount;
            // do we need to grow the unmanaged array?
            if (newTotalSegmentCount > _segments.Length)
            {
                // yes, we need to grow the unmanaged array
                _segments.Realloc(newTotalSegmentCount);
            }
            _usedSegmentCount = newTotalSegmentCount;
            // update the cluster state
            ConcurrentBitmap56 clusterState = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            if (lastSegmentAdditionalSize > 0)
            {
                UpdateStateSnapshot(ref clusterState, lastSegmentIndex);
            }
            for (int i = _usedSegmentCount - additionalSegmentCount; i < _usedSegmentCount; i++)
            {
                clusterState = clusterState.SetChildEmpty(i);
            }
            ConcurrentBitmap56.VolatileWrite(ref _clusterState, clusterState, updateToken: true);
            // update the last segment size
            _lastSegmentSize = additionalSizeForNewSegments % SEGMENT_BIT_SIZE;
            if (_lastSegmentSize == 0)
            {
                _lastSegmentSize = SEGMENT_BIT_SIZE;
            }
            stateChanged = true;
        }
        // update the bit size
        _bitSize += additionalSize;
        return stateChanged;
    }

    public override void UpdateBit(int index, bool value)
    {
        int segment = index / SEGMENT_BIT_SIZE;
        int segmentOffset = index % SEGMENT_BIT_SIZE;
        int iteration = 0;
        ConcurrentBitmap56 clusterStateSnapshot;
        do
        {
            clusterStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            if (iteration == 0)
            {
                ConcurrentBitmap56.UpdateBit(ref _segments.GetRefUnsafe(segment), segmentOffset, value);
            }
            else
            {
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in segment {segment} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (UpdateStateSnapshotIfRequired(ref clusterStateSnapshot, value, segment) && !ConcurrentBitmap56.TryWrite(ref _clusterState, clusterStateSnapshot));
    }

    public override bool TryUpdateBit(int index, byte token, bool value)
    {
        int segment = index / SEGMENT_BIT_SIZE;
        int segmentOffset = index % SEGMENT_BIT_SIZE;
        int iteration = 0;
        ConcurrentBitmap56 clusterStateSnapshot;
        do
        {
            clusterStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _clusterState);
            if (iteration == 0)
            {
                if (!ConcurrentBitmap56.TryUpdateBit(ref _segments.GetRefUnsafe(segment), token, segmentOffset, value))
                {
                    return false;
                }
            }
            else
            {
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in segment {segment} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (UpdateStateSnapshotIfRequired(ref clusterStateSnapshot, value, segment) && !ConcurrentBitmap56.TryWrite(ref _clusterState, clusterStateSnapshot));
        return true;
    }

    private bool UpdateStateSnapshotIfRequired(ref ConcurrentBitmap56 snapshot, bool value, int segment)
    {
        int segmentCapacity = segment == _usedSegmentCount - 1 ? _lastSegmentSize : SEGMENT_BIT_SIZE;
        ConcurrentBitmap56 bmp = ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(segment));
        if (!value && bmp.IsEmptyUnsafe(segmentCapacity) && !snapshot.IsChildEmpty(segment))
        {
            // we set the bit to 0, and the segment is now empty which is not yet reflected in the cluster bitmap
            // --> mark the segment as empty
            snapshot = snapshot.SetChildEmpty(segment);
        }
        else if (value && bmp.IsFullUnsafe(segmentCapacity) && !snapshot.IsChildFull(segment))
        {
            // we set the bit to 1, and the segment is now full which is not yet reflected in the cluster bitmap
            // --> mark the segment as full
            snapshot = snapshot.SetChildFull(segment);
        }
        else if (value && !bmp.IsEmptyUnsafe(segmentCapacity) && snapshot.IsChildEmpty(segment))
        {
            // we set the bit to 1, and the segment is now not empty anymore which is not yet reflected in the cluster bitmap
            // --> clear the empty bit
            snapshot = snapshot.ClearChildEmpty(segment);
        }
        else if (!value && !bmp.IsFullUnsafe(segmentCapacity) && snapshot.IsChildFull(segment))
        {
            // we set the bit to 0, and the segment is now not full anymore which is not yet reflected in the cluster bitmap
            // --> clear the full bit
            snapshot = snapshot.ClearChildFull(segment);
        }
        else
        {
            // no change
            return false;
        }
        return true;
    }

    private bool UpdateStateSnapshot(ref ConcurrentBitmap56 snapshot, int segment)
    {
        int segmentCapacity = segment == _usedSegmentCount - 1 ? _lastSegmentSize : SEGMENT_BIT_SIZE;
        ConcurrentBitmap56 segmentState = ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(segment));
        if (segmentState.IsEmptyUnsafe(segmentCapacity) && !snapshot.IsChildEmpty(segment))
        {
            // we set the bit to 0, and the segment is now empty which is not yet reflected in the cluster bitmap
            // --> mark the segment as empty
            snapshot = snapshot.SetChildEmpty(segment);
        }
        else if (segmentState.IsFullUnsafe(segmentCapacity) && !snapshot.IsChildFull(segment))
        {
            // we set the bit to 1, and the segment is now full which is not yet reflected in the cluster bitmap
            // --> mark the segment as full
            snapshot = snapshot.SetChildFull(segment);
        }
        else if (!segmentState.IsEmptyUnsafe(segmentCapacity) && snapshot.IsChildEmpty(segment) 
            || !segmentState.IsFullUnsafe(segmentCapacity) && snapshot.IsChildFull(segment))
        {
            // the segment is neither full nor empty, but the cluster bitmap says otherwise
            snapshot = snapshot.ClearChildEmpty(segment).ClearChildFull(segment);
        }
        else
        {
            // no change
            return false;
        }
        return true;
    }

    public override void InsertBitAt(int index, bool value, out bool lastBit)
    {
        // has global write lock

        // we need to shift all bits after the insertion point to the right
        // the last bit must then be inserted at the beginning of the next segment
        int segment = index / SEGMENT_BIT_SIZE;
        int segmentOffset = index % SEGMENT_BIT_SIZE;
        // the last bit gets pushed out of the segment, so we need to remember it
        // the very last bit must have been remembered by the caller
        int currentSegmentSize = SEGMENT_BIT_SIZE;
        lastBit = default;
        for (int i = segment; i < _usedSegmentCount; i++)
        {
            if (i == _usedSegmentCount - 1)
            {
                currentSegmentSize = _lastSegmentSize;
            }
            if (i == segment)
            {
                lastBit = ConcurrentBitmap56.VolatileRead(ref _segments.GetRef(segment)).IsBitSet(currentSegmentSize - 1);
                ConcurrentBitmap56.InsertBitAt(ref _segments.GetRefUnsafe(i), segmentOffset, value);
            }
            else
            {
                // not the first segment, we need to insert the last bit at the beginning of the next segment
                bool temp = ConcurrentBitmap56.VolatileRead(ref _segments.GetRef(i)).IsBitSet(currentSegmentSize - 1);
                ConcurrentBitmap56.InsertBitAt(ref _segments.GetRefUnsafe(i), 0, lastBit);
                lastBit = temp;
            }
        }
    }

    public override void RemoveBitAt(int index)
    {
        // has global write lock
        int segment = index / SEGMENT_BIT_SIZE;
        int segmentOffset = index % SEGMENT_BIT_SIZE;
        for (int i = segment; i < _usedSegmentCount; i++)
        {
            if (i == segment)
            {
                ConcurrentBitmap56.RemoveBitAt(ref _segments.GetRef(i), segmentOffset);
            }
            else
            {
                // not the first segment, we need to remove the first bit and shift the rest
                // the removed bit must then be inserted at the end of the previous segment
                bool value = ConcurrentBitmap56.VolatileRead(ref _segments.GetRef(i)).IsBitSet(0);
                ConcurrentBitmap56.RemoveBitAt(ref _segments.GetRefUnsafe(i), 0);
                ConcurrentBitmap56.UpdateBit(ref _segments.GetRefUnsafe(i - 1), SEGMENT_BIT_SIZE - 1, value);
            }
        }
    }

    internal override ConcurrentBitmap56 RefreshState(int startIndex)
    {
        // has global write lock
        ConcurrentBitmap56 clusterStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _clusterState);
        int segmentCapacity = SEGMENT_BIT_SIZE;
        int segmentIndex = startIndex / SEGMENT_BIT_SIZE;
        for (int i = segmentIndex; i < _usedSegmentCount; i++)
        {
            if (i == _usedSegmentCount - 1)
            {
                segmentCapacity = _lastSegmentSize;
            }
            ConcurrentBitmap56 segment = ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(i));
            if (segment.IsEmptyUnsafe(segmentCapacity))
            {
                clusterStateSnapshot = clusterStateSnapshot.SetChildEmpty(i);
            }
            else if (segment.IsFullUnsafe(segmentCapacity))
            {
                clusterStateSnapshot = clusterStateSnapshot.SetChildFull(i);
            }
            else
            {
                clusterStateSnapshot = clusterStateSnapshot.ClearChildFull(i).ClearChildEmpty(i);
            }
        }
        ConcurrentBitmap56.VolatileWrite(ref _clusterState, clusterStateSnapshot, updateToken: true);
        // the token may be out of date, but that's ok
        // we only care about the fullness and emptiness of the segments
        return clusterStateSnapshot;
    }

    public override int UnsafePopCount()
    {
        int count = 0;
        int segmentCapacity = SEGMENT_BIT_SIZE;
        for (int i = 0; i < _usedSegmentCount; i++)
        {
            if (i == _usedSegmentCount - 1)
            {
                segmentCapacity = _lastSegmentSize;
            }
            count += ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(i)).PopCountUnsafe(segmentCapacity);
        }
        return count;
    }

    internal override void ToString(StringBuilder sb, int depth)
    {
        sb.Append(' ', depth * 2)
            .Append($"Leaf cluster (Base offset: 0x{_baseAddress:x8}, ")
            .Append(_usedSegmentCount)
            .Append(" segments (")
            .Append(_segments.Length)
            .Append(" allocated), Total size: ")
            .Append(_bitSize)
            .Append(" bits, state: ")
            .Append(IsEmpty ? "Empty" : IsFull ? "Full" : "Partial")
            .Append(", internal cluster state: ")
            .Append(ConcurrentBitmap56.VolatileRead(ref _clusterState).ToString())
            .AppendLine(")");

        for (int i = 0; i < _segments.Length; i++)
        {
            sb.Append(' ', (depth + 1) * 2)
                .Append($"Segment offset: 0x{_baseAddress + i * SEGMENT_BIT_SIZE:x8}, ")
                .Append(ConcurrentBitmap56.VolatileRead(ref _segments.GetRefUnsafe(i)).ToString());
            if (i >= _usedSegmentCount)
            {
                sb.Append(" (reserved, not in use)");
            }
            sb.AppendLine();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            _segments.Dispose();
            disposedValue = true;
        }
    }

    ~ConcurrentBitmapClusterNode()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public override void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
