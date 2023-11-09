namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

internal class FairState : QueuingStateNode
{
    internal double VirtualExecutionTime { get; set; }

    public FairState(QueuingStateNode? inner) : base(inner)
    {
    }
}
