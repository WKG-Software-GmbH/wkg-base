using System.Text;

namespace Wkg.Collections.Concurrent.BitmapInternals;

internal abstract class ConcurrentBitmapNode : IDisposable
{
    private protected readonly ConcurrentBitmapInternalNode? _parent;
    private protected readonly int _baseAddress;
    private protected readonly int _bitSize;

    protected ConcurrentBitmapNode(int baseAddress, ConcurrentBitmapInternalNode? parent, int bitSize)
    {
        _baseAddress = baseAddress;
        _parent = parent;
        _bitSize = bitSize;
    }

    public int Length => _bitSize;

    internal abstract int NodeLength { get; }

    public abstract bool IsFull { get; }

    public abstract bool IsEmpty { get; }

    public abstract bool IsLeaf { get; }

    internal abstract ref ConcurrentBitmap56State InternalStateBitmap { get; }

    internal abstract ConcurrentBitmap56 RefreshState();

    internal abstract void ToString(StringBuilder sb, int depth);

    public abstract void UpdateBit(int index, bool value);

    public abstract byte GetToken(int index);

    public abstract bool TryUpdateBit(int index, byte token, bool value);

    public abstract bool IsBitSet(int index);

    public abstract void InsertBitAt(int index, bool value, out bool lastBit);

    public abstract void RemoveBitAt(int index);

    public abstract void Dispose();
}
