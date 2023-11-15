namespace Wkg.Threading.Workloads.Queuing.Classful.Routing;

public readonly struct RoutingPathNode<THandle>(IClassfulQdisc<THandle> qdisc, THandle handle, int offset) where THandle : unmanaged
{
    internal readonly IClassfulQdisc<THandle> Qdisc = qdisc;
    public readonly THandle Handle = handle;
    public readonly int Offset = offset;
}