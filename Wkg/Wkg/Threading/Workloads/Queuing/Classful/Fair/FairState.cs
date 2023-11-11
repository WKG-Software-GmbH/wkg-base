namespace Wkg.Threading.Workloads.Queuing.Classful.Fair;

internal class FairState : QueuingStateNode
{
    internal double VirtualExecutionTime { get; set; }

    public FairState(QueuingStateNode? inner) : base(inner)
    {
    }
}
