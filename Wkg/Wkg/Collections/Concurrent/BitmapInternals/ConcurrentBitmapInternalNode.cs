using System.Text;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

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

internal class ConcurrentBitmapInternalNode : ConcurrentBitmapNode
{
    // each node must track the fullness and emptiness of its clusters, so 2 bits are required per cluster
    // bits 0 to 27 are used for the cluster emptiness state,
    // bits 28 to 55 are used for the cluster fullness state
    private ConcurrentBitmap56State _nodeState;
    private readonly ConcurrentBitmapNode[] _children;
    private readonly int _childBitSize;

    public ConcurrentBitmapInternalNode(int baseAddress, int remainingDepth, int bitSize, ConcurrentBitmapInternalNode? parent) : base(baseAddress, parent, bitSize)
    {
        // do we need more internal nodes, or are we one level above the leaf nodes?
        if (bitSize > INTERNAL_NODE_BIT_LIMIT)
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
            _children = new ConcurrentBitmapNode[childCount];
            int remainingBits = bitSize;
            for (int i = 0; i < _children.Length; i++, remainingBits -= childStepSize)
            {
                int childBitSize;
                if (i == _children.Length - 1)
                {
                    // last child
                    childBitSize = remainingBits;
                }
                else
                {
                    childBitSize = childStepSize;
                }
                _children[i] = new ConcurrentBitmapInternalNode(baseAddress + i * childStepSize, remainingDepth - 1, childBitSize, this);
            }
            _childBitSize = childStepSize;
        }
        else
        {
            // we are one level above the leaf nodes
            // split the bitSize into multiple clusters
            int childCount = (bitSize + CLUSTER_BIT_SIZE - 1) / CLUSTER_BIT_SIZE;
            _children = new ConcurrentBitmapNode[childCount];
            int remainingBits = bitSize;
            for (int i = 0; i < _children.Length; i++, remainingBits -= CLUSTER_BIT_SIZE)
            {
                int childBitSize;
                if (i == _children.Length - 1)
                {
                    // last child
                    childBitSize = remainingBits;
                }
                else
                {
                    childBitSize = CLUSTER_BIT_SIZE;
                }
                _children[i] = new ConcurrentBitmapClusterNode(baseAddress + i * CLUSTER_BIT_SIZE, childBitSize, this);
            }
            _childBitSize = CLUSTER_BIT_SIZE;
        }

        // initialize the node state
        ConcurrentBitmap56 state = default;
        for (int i = 0; i < _children.Length; i++)
        {
            state = state.SetChildEmpty(i);
        }
        ConcurrentBitmap56.VolatileWrite(ref _nodeState, state);
    }

    public override bool IsLeaf => false;

    public override bool IsFull => ConcurrentBitmap56.VolatileRead(ref _nodeState).AreChildrenFull(_children.Length);

    public override bool IsEmpty => ConcurrentBitmap56.VolatileRead(ref _nodeState).AreChildrenEmpty(_children.Length);

    internal override ref ConcurrentBitmap56State InternalStateBitmap => ref _nodeState;

    internal override int NodeLength => _children.Length;

    public override byte GetToken(int index) => _children[index / _childBitSize].GetToken(index % _childBitSize);

    public override bool IsBitSet(int index) => _children[index / _childBitSize].IsBitSet(index % _childBitSize);

    public override void UpdateBit(int index, bool value)
    {
        int child = index / _childBitSize;
        int childOffset = index % _childBitSize;
        int iteration = 0;
        ConcurrentBitmap56 nodeBmpSnapshot;
        do
        {
            nodeBmpSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (iteration == 0)
            {
                _children[child].UpdateBit(childOffset, value);
            }
            else
            {
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in child {child} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (UpdateStateSnapshotIfRequired(ref nodeBmpSnapshot, value, child) && !ConcurrentBitmap56.TryWrite(ref _nodeState, nodeBmpSnapshot));
    }

    public override bool TryUpdateBit(int index, byte token, bool value)
    {
        int child = index / _childBitSize;
        int childOffset = index % _childBitSize;
        int iteration = 0;
        ConcurrentBitmap56 nodeStateSnapshot;
        do
        {
            nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
            if (iteration == 0)
            {
                if (!_children[child].TryUpdateBit(childOffset, token, value))
                {
                    return false;
                }
            }
            else
            {
                DebugLog.WriteDiagnostic($"Retrying update of bit {index} in child {child} (iteration {iteration}).", LogWriter.Blocking);
            }
            iteration++;
        } while (UpdateStateSnapshotIfRequired(ref nodeStateSnapshot, value, child) && !ConcurrentBitmap56.TryWrite(ref _nodeState, nodeStateSnapshot));
        return true;
    }

    private bool UpdateStateSnapshotIfRequired(ref ConcurrentBitmap56 snapshot, bool value, int child)
    {
        int childCapacity = child == _children.Length - 1 ? _children[^1].NodeLength : SEGMENTS_PER_CLUSTER;
        ConcurrentBitmap56 childState = ConcurrentBitmap56.VolatileRead(ref _children[child].InternalStateBitmap);
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

    public override void RemoveBitAt(int index)
    {
        // have global write lock
        int childStart = index / _childBitSize;
        int childOffset = index % _childBitSize;
        for (int i = childStart; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children[i];
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
                _children[i - 1].UpdateBit(_children[i - 1].Length - 1, value);
            }
        }
    }

    public override void InsertBitAt(int index, bool value, out bool lastBit)
    {
        // have global write lock
        // we need to shift all bits after the insertion point to the right
        // the last bit must then be inserted at the beginning of the next child
        int childStart = index / _childBitSize;
        int childOffset = index % _childBitSize;
        // the last bit gets pushed out of the child, so we need to remember it
        // the very last bit must have been remembered by the caller
        lastBit = default;
        for (int i = childStart; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children[i];
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

    internal override ConcurrentBitmap56 RefreshState()
    {
        // has global write lock
        ConcurrentBitmap56 nodeStateSnapshot = ConcurrentBitmap56.VolatileRead(ref _nodeState);
        for (int i = 0; i < _children.Length; i++)
        {
            ConcurrentBitmapNode child = _children[i];
            ConcurrentBitmap56 childState = child.RefreshState();
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

    internal override void ToString(StringBuilder sb, int depth)
    {
        sb.Append(' ', depth * 2)
            .Append($"InternalNode (Base address: 0x{_baseAddress:x8}, ")
            .Append(_children.Length)
            .Append(" children, state: ")
            .Append(IsEmpty ? "Empty" : IsFull ? "Full" : "Partial")
            .Append(", internal node state: ")
            .Append(ConcurrentBitmap56.VolatileRead(ref _nodeState).ToString())
            .AppendLine(")");

        foreach (ConcurrentBitmapNode child in _children)
        {
            child.ToString(sb, depth + 1);
        }
    }

    public override void Dispose()
    {
        foreach (ConcurrentBitmapNode child in _children)
        {
            child.Dispose();
        }
    }
}
