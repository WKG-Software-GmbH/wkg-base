using System.Diagnostics.CodeAnalysis;
using Wkg.Data.Pooling;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Queuing.Routing;

public ref struct RoutingPath<THandle> where THandle : unmanaged
{
    private PooledArray<RoutingPathNode<THandle>> _path;
    private IClassifyingQdisc<THandle>? _leaf;

    internal RoutingPath(int capacity)
    {
        _path = ArrayPool.Rent<RoutingPathNode<THandle>>(capacity);
        _path.TryResizeUnsafe(0, out _path);
    }

    /// <summary>
    /// Gets the node at the specified index.
    /// </summary>
    /// <param name="index">The index of the node to get.</param>
    /// <returns>A reference to the node at the specified index.</returns>
    public readonly ref readonly RoutingPathNode<THandle> this[int index] => ref _path[index];

    /// <summary>
    /// Gets the number of nodes in the path.
    /// </summary>
    public readonly int Count => _path.Length;

    /// <summary>
    /// Gets the leaf qdisc of the path. This is the qdisc that will be used to enqueue workloads.
    /// </summary>
    public readonly IClassifyingQdisc<THandle>? Leaf => _leaf;

    /// <summary>
    /// Adds a node to the path.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void Add(RoutingPathNode<THandle> node)
    {
        PooledArray<RoutingPathNode<THandle>> path = _path;
        if (!path.TryResizeUnsafe(path.Length + 1, out PooledArray<RoutingPathNode<THandle>> resized))
        {
            int newLength = path.Length * 2;
            PooledArray<RoutingPathNode<THandle>> newPath = ArrayPool.Rent<RoutingPathNode<THandle>>(newLength);
            path.Array.AsSpan().CopyTo(newPath.Array.AsSpan());
            _path = new PooledArray<RoutingPathNode<THandle>>(newPath.Array, path.Length + 1, noChecks: true);
        }
        resized.Array[path.Length] = node;
        _path = resized;
    }

    /// <summary>
    /// Completes the routing path by setting the leaf qdisc.
    /// </summary>
    /// <param name="leaf">The leaf qdisc. This is the qdisc that will be used to enqueue workloads.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="leaf"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the routing path has already been completed.</exception>
    [MemberNotNull(nameof(_leaf))]
    public void Complete(IClassifyingQdisc<THandle> leaf)
    {
        ArgumentNullException.ThrowIfNull(leaf, nameof(leaf));
        WorkloadSchedulingException.ThrowIfRoutingPathLeafIsCompleted(_leaf);
        _leaf = leaf;
    }

    public readonly Enumerator GetEnumerator() => new(this);

    public readonly void Dispose() => ArrayPool.Return(_path);

    public ref struct Enumerator
    {
        private readonly PooledArray<RoutingPathNode<THandle>> _path;
        private int _index;

        public Enumerator(RoutingPath<THandle> path)
        {
            _path = path._path;
            _index = _path.Length;
        }

        public readonly ref readonly RoutingPathNode<THandle> Current => ref _path.Array[_index];

        public bool MoveNext() => --_index >= 0;
    }
}
