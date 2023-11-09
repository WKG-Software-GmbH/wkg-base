namespace Wkg.Threading.Workloads.Queuing.Classful.Routing;

public readonly struct RoutingPathNode<THandle> where THandle : unmanaged
{
    internal readonly IClassfulQdisc<THandle> Qdisc;
    public readonly THandle Handle;
    public readonly int Offset;

    public RoutingPathNode(IClassfulQdisc<THandle> qdisc, THandle handle, int offset)
    {
        Qdisc = qdisc;
        Handle = handle;
        Offset = offset;
    }
}