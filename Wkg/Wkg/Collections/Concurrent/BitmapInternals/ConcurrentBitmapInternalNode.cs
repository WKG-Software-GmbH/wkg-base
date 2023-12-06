using System.Diagnostics;
using System.Text;
using Wkg.Common;
using Wkg.Data.Pooling;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Collections.Concurrent.BitmapInternals;

using static ConcurrentBitmap;

internal class ConcurrentBitmapInternalNode : ConcurrentBitmapNode
{
    // each node must track the fullness and emptiness of its clusters, so 2 bits are required per cluster
    // bits 0 to 27 are used for the cluster emptiness state,
    // bits 28 to 55 are used for the cluster fullness state
    private ConcurrentBitmap56State _nodeState;
    // we abuse a pooled array to be able to resize the array within boundaries of the actual underlying array size
    // if we free a child node, we can simply soft-resize the array that way and reclaim the space later as needed
    private PooledArray<ConcurrentBitmapNode> _children;
    private readonly int _childMaxBitSize;
    private readonly bool _childIsInternalNode;
    private readonly int _depth;

    public ConcurrentBitmapInternalNode(int externalNodeIndex, int baseAddress, int remainingDepth, int bitSize, IParentNode parent, ConcurrentBitmapNode? oldRoot) : base(externalNodeIndex, baseAddress, parent, bitSize)
    {
        // do we need more internal nodes, or are we one level above the leaf nodes?
        _childIsInternalNode = bitSize > INTERNAL_NODE_BIT_LIMIT;
        _depth = remainingDepth;
        if (_childIsInternalNode)
        {
            // we need another level of internal nodes
            // split the bitSize into multiple children such that we are close to the maximum number of children
            int childStepSize = SEGMENT_BIT_SIZE;
            for (int i = 0; i < remainingDepth; i++)
            {
                // see how many hierachical levels we need for the remaining bits
                childStepSize *= SEGMENTS_PER_CLUSTER;
            }
            int childCount = (bitSize + childStepSize - 1) / childStepSize;
            _children = new PooledArray<ConcurrentBitmapNode>(new ConcurrentBitmapNode[childCount], childCount);
            Span<ConcurrentBitmapNode> children = _children.AsSpan();
            int remainingBits = bitSize;
            for (int i = 0; i < children.Length; i++, remainingBits -= childStepSize)
            {
                if (oldRoot != null && i == 0)
                {
                    // we are reusing an old root node
                    children[i] = oldRoot;
                }
                else
                {
                    int childBitSize;
                    if (i == children.Length - 1)
                    {
                        // last child
                        childBitSize = remainingBits;
                    }
                    else
                    {
                        childBitSize = childStepSize;
                    }
                    children[i] = new ConcurrentBitmapInternalNode(i, baseAddress + i * childStepSize, remainingDepth - 1, childBitSize, this, null);
                }
            }
            _childMaxBitSize = childStepSize;
        }
        else
        {
            // we are one level above the leaf nodes
            // split the bitSize into multiple clusters
            int childCount = (bitSize + CLUSTER_BIT_SIZE - 1) / CLUSTER_BIT_SIZE;
            _children = new PooledArray<ConcurrentBitmapNode>(new ConcurrentBitmapNode[childCount], childCount);
            Span<ConcurrentBitmapNode> children = _children.AsSpan();
            int remainingBits = bitSize;
            for (int i = 0; i < children.Length; i++, remainingBits -= CLUSTER_BIT_SIZE)
            {
                if (oldRoot != null && i == 0)
                {
                    // we are reusing an old root node
                    children[i] = oldRoot;
                }
                else
                {
                    int childBitSize;
                    if (i == children.Length - 1)
                    {
                        // last child
                        childBitSize = remainingBits;
                    }
                    else
                    {
                        childBitSize = CLUSTER_BIT_SIZE;
                    }
                    children[i] = new ConcurrentBitmapClusterNode(i, baseAddress + i * CLUSTER_BIT_SIZE, childBitSize, this);
                }
            }
            _childMaxBitSize = CLUSTER_BIT_SIZE;
        }

        // initialize the node state
        // we could have non-empty children, so we need to initialize the node state
        ConcurrentBitmap56 state = default;
        for (int i = 0; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            if (child.IsFull)
            {
                state = state.SetChildFull(i);
            }
            else if (child.IsEmpty)
            {
                state = state.SetChildEmpty(i);
            }
            else
            {
                state = state.ClearChildFull(i).ClearChildEmpty(i);
            }
        }
        ConcurrentBitmap56.VolatileWrite(ref _nodeState, state);
    }

    public override int MaxNodeBitLength => _childMaxBitSize * SEGMENTS_PER_CLUSTER;

    public override bool IsLeaf => false;

    public override bool IsFull => ConcurrentBitmap56.VolatileRead(ref _nodeState).AreChildrenFull(_children.Length);

    public override bool IsEmpty => ConcurrentBitmap56.VolatileRead(ref _nodeState).AreChildrenEmpty(_children.Length);

    internal override ref ConcurrentBitmap56State InternalStateBitmap => ref _nodeState;

    internal override int NodeLength => _children.Length;

    // TODO: we can probably optimize these modulo operations as we know that the values are always of a certain step size
    public override byte GetToken(int index) => _children.Array[index / _childMaxBitSize].GetToken(index % _childMaxBitSize);

    public override bool IsBitSet(int index) => _children.Array[index / _childMaxBitSize].IsBitSet(index % _childMaxBitSize);

    public override GuardedBitInfo GetBitInfo(int index) => _children.Array[index / _childMaxBitSize].GetBitInfo(index % _childMaxBitSize);

    protected override void ReplaceChildNode(int index, ConcurrentBitmapNode newNode)
    {
        Debug.Assert(index >= 0 && index < _children.Length);
        _children.Array[index] = newNode;
    }

    public override void UpdateBit(int index, bool value)
    {
        int child = index / _childMaxBitSize;
        int childOffset = index % _childMaxBitSize;
        int iteration = 0;
        ConcurrentBitmap56 nodeStateSnapshot;
        do
        {
            nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (iteration == 0)
            {
                _children.Array[child].UpdateBit(childOffset, value);
            }
            else
            {
                bool newValue = _children.Array[child].IsBitSet(childOffset);
                if (newValue != value)
                {
                    // our value is no longer relevant, (another write operation wrote to the same segment)
                    return;
                }
                Debug.Assert(iteration < 100, "Too many iterations, something is wrong.");
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in child {child} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (ConcurrentBitmap56.VolatileRead(ref _nodeState).GetToken() != nodeStateSnapshot.GetToken()
            || UpdateStateSnapshotIfRequired(ref nodeStateSnapshot, value, child) && !ConcurrentBitmap56.TryWrite(ref _nodeState, nodeStateSnapshot));
    }

    public override bool TryUpdateBit(int index, byte token, bool value)
    {
        int child = index / _childMaxBitSize;
        int childOffset = index % _childMaxBitSize;
        int iteration = 0;
        ConcurrentBitmap56 nodeStateSnapshot;
        do
        {
            nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (iteration == 0)
            {
                if (!_children.Array[child].TryUpdateBit(childOffset, token, value))
                {
                    return false;
                }
            }
            else
            {
                // we updated the segment, but the nodeState changed in the meantime
                // we need to resample the value we wrote earlier (which may have been overwritten by another thread)
                // based on the single resampled value, we can update the clusterstate
                GuardedBitInfo bitInfo = _children.Array[child].GetBitInfo(childOffset);
                if (bitInfo.Token != token)
                {
                    // our value is no longer relevant, (another write operation wrote to the same segment)
                    return false;
                }
                bool newValue = bitInfo.IsSet;
                // if the value is the same as before (e.g., the value we wrote is still there), we need to update the clusterstate accordingly
                // otherwise something is wrong, because the token should have been updated as well
                Debug.Assert(newValue == value, "The token should have been updated as well.");
                Debug.Assert(iteration < 100, "Too many iterations, something is wrong.");
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in child {child} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (ConcurrentBitmap56.VolatileRead(ref _nodeState).GetToken() != nodeStateSnapshot.GetToken()
            || UpdateStateSnapshotIfRequired(ref nodeStateSnapshot, value, child) && !ConcurrentBitmap56.TryWrite(ref _nodeState, nodeStateSnapshot));
        return true;
    }

    private bool UpdateStateSnapshotIfRequired(ref ConcurrentBitmap56 snapshot, bool value, int child)
    {
        ConcurrentBitmapNode childNode = _children.Array[child];
        int childCapacity = childNode.NodeLength;
        ConcurrentBitmap56 childState = ConcurrentBitmap56.VolatileRead(ref childNode.InternalStateBitmap);
        if (!value && childState.AreChildrenEmpty(childCapacity) && !snapshot.IsChildEmpty(child))
        {
            // we set the bit to 0, and the child is now empty which is not yet reflected in the cluster bitmap
            // --> mark the child as empty
            snapshot = snapshot.SetChildEmpty(child);
        }
        else if (value && childState.AreChildrenFull(childCapacity) && !snapshot.IsChildFull(child))
        {
            // we set the bit to 1, and the child is now full which is not yet reflected in the cluster bitmap
            // --> mark the child as full
            snapshot = snapshot.SetChildFull(child);
        }
        else if (value && !childState.AreChildrenEmpty(childCapacity) && snapshot.IsChildEmpty(child))
        {
            // we set the bit to 1, and the child is now not empty anymore which is not yet reflected in the cluster bitmap
            // --> clear the empty bit
            snapshot = snapshot.ClearChildEmpty(child);
        }
        else if (!value && !childState.AreChildrenFull(childCapacity) && snapshot.IsChildFull(child))
        {
            // we set the bit to 0, and the child is now not full anymore which is not yet reflected in the cluster bitmap
            // --> clear the full bit
            snapshot = snapshot.ClearChildFull(child);
        }
        else
        {
            // no change
            return false;
        }
        return true;
    }

    private bool UpdateStateSnapshot(ref ConcurrentBitmap56 snapshot, int child)
    {
        ConcurrentBitmapNode childNode = _children.Array[child];
        int childCapacity = childNode.NodeLength;
        ConcurrentBitmap56 childState = ConcurrentBitmap56.VolatileRead(ref childNode.InternalStateBitmap);
        if (childState.AreChildrenEmpty(childCapacity) && !snapshot.IsChildEmpty(child))
        {
            // we set the bit to 0, and the child is now empty which is not yet reflected in the cluster bitmap
            // --> mark the child as empty
            snapshot = snapshot.SetChildEmpty(child);
        }
        else if (childState.AreChildrenFull(childCapacity) && !snapshot.IsChildFull(child))
        {
            // we set the bit to 1, and the child is now full which is not yet reflected in the cluster bitmap
            // --> mark the child as full
            snapshot = snapshot.SetChildFull(child);
        }
        else if (!childState.AreChildrenEmpty(childCapacity) && snapshot.IsChildEmpty(child) 
            || !childState.AreChildrenFull(childCapacity) && snapshot.IsChildFull(child))
        {
            // we set the bit to 1, and the child is now not empty anymore which is not yet reflected in the cluster bitmap
            // --> clear the empty bit
            snapshot = snapshot.ClearChildEmpty(child).ClearChildFull(child);
        }
        else
        {
            // no change
            return false;
        }
        return true;
    }

    public override void RemoveBitAt(int index)
    {
        // have global write lock
        int childStart = index / _childMaxBitSize;
        int childOffset = index % _childMaxBitSize;
        for (int i = childStart; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            if (i == childStart)
            {
                child.RemoveBitAt(childOffset);
            }
            else
            {
                // not the first child, we need to remove the first bit and shift the rest
                // the removed bit must then be inserted at the end of the previous child
                bool value = child.IsBitSet(0);
                child.RemoveBitAt(0);
                _children.Array[i - 1].UpdateBit(_children.Array[i - 1].Length - 1, value);
            }
        }
    }

    public override void InsertBitAt(int index, bool value, out bool lastBit)
    {
        // have global write lock
        // we need to shift all bits after the insertion point to the right
        // the last bit must then be inserted at the beginning of the next child
        int childStart = index / _childMaxBitSize;
        int childOffset = index % _childMaxBitSize;
        // the last bit gets pushed out of the child, so we need to remember it
        // the very last bit must have been remembered by the caller
        lastBit = default;
        for (int i = childStart; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            if (i == childStart)
            {
                child.InsertBitAt(childOffset, value, out lastBit);
            }
            else
            {
                // not the first child, we need to insert the last bit at the beginning of the next child
                child.InsertBitAt(0, lastBit, out lastBit);
            }
        }
    }

    internal override bool Grow(int additionalSize)
    {
        // has global write lock
        Debug.Assert(additionalSize >= 0);
        Debug.Assert(_bitSize + additionalSize <= MaxNodeBitLength);

        if (additionalSize == 0)
        {
            // nothing to do
            return false;
        }

        // check if we can simply grow the last child, or if we need to add new children
        int oldLastChildIndex = _children.Length - 1;
        bool stateChanged = false;
        // oldLastChildIndex can be -1 if we are growing an empty node
        if (oldLastChildIndex != -1 && _children.Array[oldLastChildIndex].Length + additionalSize <= _childMaxBitSize)
        {
            // we can simply grow the last child
            ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (_children.Array[oldLastChildIndex].Grow(additionalSize) && UpdateStateSnapshot(ref nodeStateSnapshot, oldLastChildIndex))
            {
                // state of the last child changed
                ConcurrentBitmap56.VolatileWrite(ref _nodeState, nodeStateSnapshot, updateToken: true);
                stateChanged = true;
            }
        }
        else
        {
            // we need to add new children
            // how many new children do we need?
            int newTotalBitSize = _bitSize + additionalSize;
            int newTotalChildCount = (newTotalBitSize + _childMaxBitSize - 1) / _childMaxBitSize;
            int newChildCount = newTotalChildCount - _children.Length;
            Debug.Assert(newChildCount > 0);
            // do we need to grow the array, or can we resize the existing array?
            if (!_children.TryResize(newTotalChildCount, out _children))
            {
                // grow the array
                ConcurrentBitmapNode[] newChildren = new ConcurrentBitmapNode[newTotalChildCount];
                Array.Copy(_children.Array, newChildren, _children.Array.Length);
                _children = new PooledArray<ConcurrentBitmapNode>(newChildren, newChildren.Length);
            }
            Span<ConcurrentBitmapNode> children = _children.AsSpan();
            int remainingBits = additionalSize;
            ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            for (int i = FastMath.Max(oldLastChildIndex, 0); i < children.Length; i++, remainingBits -= _childMaxBitSize)
            {
                // we need to grow the old last child, and add new children
                if (i == oldLastChildIndex)
                {
                    // grow the old last child
                    children[i].Grow(_childMaxBitSize - children[i].Length);
                }
                else
                {
                    // add new children
                    int childBitSize;
                    if (i == children.Length - 1)
                    {
                        // last child
                        childBitSize = remainingBits;
                    }
                    else
                    {
                        childBitSize = _childMaxBitSize;
                    }
                    // what type of child do we need?
                    int baseAddress = _baseAddress + i * _childMaxBitSize;
                    if (_childIsInternalNode)
                    {
                        children[i] = new ConcurrentBitmapInternalNode(i, baseAddress, _depth - 1, childBitSize, this, null);
                    }
                    else
                    {
                        children[i] = new ConcurrentBitmapClusterNode(i, baseAddress, childBitSize, this);
                    }
                }
            }
            for (int i = FastMath.Max(oldLastChildIndex, 0); i < children.Length; i++)
            {
                UpdateStateSnapshot(ref nodeStateSnapshot, i);
            }
            ConcurrentBitmap56.VolatileWrite(ref _nodeState, nodeStateSnapshot, updateToken: true);
            stateChanged = true;
        }
        _bitSize += additionalSize;
        return stateChanged;
    }

    internal override bool Shrink(int removalSize)
    {
        // has global write lock
        Debug.Assert(removalSize > 0);
        Debug.Assert(_bitSize - removalSize >= 0);

        // check if we can simply shrink the last child, or if we need to remove children
        int oldLastChildIndex = _children.Length - 1;
        bool stateChanged = false;
        ConcurrentBitmapNode lastChild = _children.Array[oldLastChildIndex];
        if (lastChild.Length - removalSize > 0)
        {
            // we can simply shrink the last child
            ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (lastChild.Shrink(removalSize) && UpdateStateSnapshot(ref nodeStateSnapshot, oldLastChildIndex))
            {
                // state of the last child changed
                ConcurrentBitmap56.VolatileWrite(ref _nodeState, nodeStateSnapshot, updateToken: true);
                stateChanged = true;
            }
        }
        else
        {
            // we need to remove children
            // how many children do we need to remove?
            Span<ConcurrentBitmapNode> children = _children.AsSpan();
            int newTotalBitSize = _bitSize - removalSize;
            int newTotalChildCount = (newTotalBitSize + _childMaxBitSize - 1) / _childMaxBitSize;
            int removedChildCount = children.Length - newTotalChildCount;
            Debug.Assert(removedChildCount > 0);
            int newLastChildIndex = newTotalChildCount - 1;
            int newLastChildSize = newTotalBitSize - newLastChildIndex * _childMaxBitSize;
            ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            for (int i = FastMath.Max(newLastChildIndex, 0); i < children.Length; i++)
            {
                // we need to shrink the old last child, and remove children
                if (i == newLastChildIndex)
                {
                    // shrink the old last child
                    if (newLastChildSize < children[i].Length && children[i].Shrink(children[i].Length - newLastChildSize))
                    {
                        // state of the last child changed
                        UpdateStateSnapshot(ref nodeStateSnapshot, i);
                    }
                }
                else
                {
                    // remove children
                    children[i].Dispose();
                    children[i] = null!;
                    nodeStateSnapshot = nodeStateSnapshot.ClearChildEmpty(i).ClearChildFull(i);
                }
            }
            bool success = _children.TryResize(newTotalChildCount, out _children);
            Debug.Assert(success);
            stateChanged = true;
            ConcurrentBitmap56.VolatileWrite(ref _nodeState, nodeStateSnapshot, updateToken: true);
            if (newTotalChildCount == 1 && _parent is not ConcurrentBitmapNode)
            {
                // we are degenerating into a linked list and we are the root node
                // we need to replace ourselves with the only child
                _parent.ReplaceChildNode(_externalNodeIndex, _children[0]);
            }
        }
        _bitSize -= removalSize;
        return stateChanged;
    }

    internal override ConcurrentBitmap56 RefreshState(int startIndex)
    {
        // has global write lock
        int childStart = startIndex / _childMaxBitSize;
        int childStartOffset = startIndex % _childMaxBitSize;
        ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
        for (int i = childStart; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            ConcurrentBitmap56 childState;
            if (i == childStart)
            {
                // the first child may have an offset, so it may be only partially affected
                childState = child.RefreshState(childStartOffset);
            }
            else
            {
                // all other children are completely affected
                childState = child.RefreshState(0);
            }
            if (childState.AreChildrenEmpty(child.NodeLength))
            {
                nodeStateSnapshot = nodeStateSnapshot.SetChildEmpty(i);
            }
            else if (childState.AreChildrenFull(child.NodeLength))
            {
                nodeStateSnapshot = nodeStateSnapshot.SetChildFull(i);
            }
            else
            {
                nodeStateSnapshot = nodeStateSnapshot.ClearChildFull(i).ClearChildEmpty(i);
            }
        }
        ConcurrentBitmap56.VolatileWrite(ref _nodeState, nodeStateSnapshot, updateToken: true);
        // the token may be out of date, but that's ok
        // we only care about the fullness and emptiness of the children
        return nodeStateSnapshot;
    }

    public override int UnsafePopCount()
    {
        int count = 0;
        for (int i = 0; i < _children.Length; i++)
        {
            count += _children.Array[i].UnsafePopCount();
        }
        return count;
    }

    internal override void ToString(StringBuilder sb, int depth)
    {
        sb.Append(' ', depth * 2)
            .Append($"InternalNode (Base offset: 0x{_baseAddress:x8}, ")
            .Append(_children.Length)
            .Append(" children, state: ")
            .Append(IsEmpty ? "Empty" : IsFull ? "Full" : "Partial")
            .Append(", internal node state: ")
            .Append(ConcurrentBitmap56.VolatileRead(ref _nodeState).ToString())
            .AppendLine(")");

        for (int i = 0; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            child.ToString(sb, depth + 1);
        }
        for (int i = _children.Length; i < _children.Array.Length; i++)
        {
            sb.Append(' ', (depth + 1) * 2)
                .Append("Allocated node (reserved, not in use): ")
                .Append(i)
                .AppendLine();
        }
    }

    public override void Dispose()
    {
        for (int i = 0; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children.Array[i];
            child.Dispose();
        }
    }
}
