namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

internal class EarliestDueDateState : QueuingStateNode
{
    internal double VirtualFinishTime { get; set; }

    public EarliestDueDateState(QueuingStateNode? inner) : base(inner)
    {
    }
}
