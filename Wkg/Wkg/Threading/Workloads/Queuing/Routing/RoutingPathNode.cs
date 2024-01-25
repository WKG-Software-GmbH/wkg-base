using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Routing;

/// <summary>
/// Represents a node in a <see cref="RoutingPath{THandle}"/>.
/// </summary>
/// <typeparam name="THandle">The type of the handle used by the qdisc.</typeparam>
/// <param name="qdisc">The qdisc that contains any child qdisc matching the handle.</param>
/// <param name="handle">The handle of the child qdisc.</param>
/// <param name="offset">The qdisc-specific offset of the child qdisc. The meaning of this value is qdisc-specific, but it is typically used to indicate the offset of the child qdisc in the parent qdisc's internal child collection data structure.</param>
public readonly struct RoutingPathNode<THandle>(IClassifyingQdisc<THandle> qdisc, THandle handle, int offset) where THandle : unmanaged
{
    internal readonly IClassifyingQdisc<THandle> Qdisc = qdisc;

    /// <summary>
    /// The handle of the child qdisc.
    /// </summary>
    public readonly THandle Handle = handle;

    /// <summary>
    /// The qdisc-specific offset of the child qdisc.
    /// </summary>
    /// <remarks>
    /// The meaning of this value is qdisc-specific, but it is typically used to indicate the offset of the child qdisc in the parent qdisc's internal child collection data structure.
    /// </remarks>
    public readonly int Offset = offset;
}