namespace Wkg.Threading.Workloads.Queuing;

public abstract class QueuingStateNode
{
    private readonly QueuingStateNode? _inner;

    private protected QueuingStateNode(QueuingStateNode? inner)
    {
        _inner = inner;
    }

    public QueuingStateNode? Strip() => _inner;
}
