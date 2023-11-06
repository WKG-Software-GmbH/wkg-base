namespace Wkg.Threading.Workloads.Queuing;

internal abstract class QueuingStateNode
{
    private readonly QueuingStateNode? _inner;

    protected QueuingStateNode(QueuingStateNode? inner)
    {
        _inner = inner;
    }

    public QueuingStateNode? Strip() => _inner;
}
