using System.Text;

namespace Wkg.Collections.Concurrent.BitmapInternals;

internal interface IParentNode
{
    internal void ReplaceChildNode(int index, ConcurrentBitmapNode newNode);
}

internal abstract class ConcurrentBitmapNode : IDisposable, IParentNode
{
    private protected readonly IParentNode _parent;
    private protected readonly int _baseAddress;
    private protected int _bitSize;
    private protected int _externalNodeIndex;


    protected ConcurrentBitmapNode(int externalNodeIndex, int baseAddress, IParentNode parent, int bitSize)
    {
        _externalNodeIndex = externalNodeIndex;
        _baseAddress = baseAddress;
        _parent = parent;
        _bitSize = bitSize;
    }

    public abstract int MaxNodeBitLength { get; }

    public int Length => _bitSize;

    internal abstract int NodeLength { get; }

    public abstract bool IsFull { get; }

    public abstract bool IsEmpty { get; }

    public abstract bool IsLeaf { get; }

    internal abstract ref ConcurrentBitmap56State InternalStateBitmap { get; }

    internal abstract bool Grow(int additionalSize);

    internal abstract bool Shrink(int removalSize);

    internal abstract ConcurrentBitmap56 RefreshState(int startIndex);

    internal abstract void ToString(StringBuilder sb, int depth);

    public abstract void UpdateBit(int index, bool value);

    public abstract int UnsafePopCount();

    public abstract byte GetToken(int index);

    public abstract bool TryUpdateBit(int index, byte token, bool value);

    public abstract bool IsBitSet(int index);

    public abstract void InsertBitAt(int index, bool value, out bool lastBit);

    public abstract void RemoveBitAt(int index);

    public abstract void Dispose();

    protected virtual void ReplaceChildNode(int index, ConcurrentBitmapNode newNode) => Pass();

    void IParentNode.ReplaceChildNode(int index, ConcurrentBitmapNode newNode) => ReplaceChildNode(index, newNode);
}
