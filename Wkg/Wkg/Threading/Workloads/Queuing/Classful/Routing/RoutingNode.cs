using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.Routing;

public ref struct RoutingPath<THandle> where THandle : unmanaged
{
    private RoutingPathNode<THandle>[] _path;
    private IClasslessQdisc? _leaf;
    private int _count;

    internal RoutingPath(int capacity)
    {
        _path = ArrayPool<RoutingPathNode<THandle>>.Shared.Rent(capacity);
        _count = 0;
    }

    public readonly ref RoutingPathNode<THandle> this[int index] => ref _path[index];

    public readonly int Count => _count;

    public readonly IClasslessQdisc? Leaf => _leaf;

    public void Add(RoutingPathNode<THandle> node)
    {
        if (_count == _path.Length)
        {
            Grow();
        }
        _path[_count++] = node;
    }

    [MemberNotNull(nameof(_leaf))]
    public void Complete(IClasslessQdisc leaf)
    {
        Throw.ArgumentNullException.IfNull(leaf, nameof(leaf));
        if (_leaf is not null)
        {
            throw new InvalidOperationException("The routing path has already been completed.");
        }
        _leaf = leaf;
    }

    private void Grow()
    {
        RoutingPathNode<THandle>[] newNodes = ArrayPool<RoutingPathNode<THandle>>.Shared.Rent(_path.Length * 2);
        Array.Copy(_path, newNodes, _path.Length);
        RoutingPathNode<THandle>[] oldNodes = _path;
        _path = newNodes;
        ArrayPool<RoutingPathNode<THandle>>.Shared.Return(oldNodes);
    }

    public readonly Enumerator GetEnumerator() => new(this);

    public ref struct Enumerator
    {
        private readonly RoutingPath<THandle> _path;
        private int _index;

        public Enumerator(RoutingPath<THandle> path)
        {
            _path = path;
            _index = path._count;
        }

        public readonly ref RoutingPathNode<THandle> Current => ref _path[_index];

        public bool MoveNext() => --_index >= 0;
    }
}
