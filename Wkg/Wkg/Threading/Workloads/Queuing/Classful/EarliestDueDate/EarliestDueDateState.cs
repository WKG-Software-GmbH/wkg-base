namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

internal class EarliestDueDateState : QueuingStateNode
{
    internal double VirtualExecutionTime { get; set; }

    internal double ExpectedExecutionTime { get; set; }

    public EarliestDueDateState(QueuingStateNode? inner) : base(inner)
    {
    }
}
