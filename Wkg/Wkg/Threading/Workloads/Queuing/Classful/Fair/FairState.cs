namespace Wkg.Threading.Workloads.Queuing.Classful.Fair;

internal class FairState(QueuingStateNode? inner) : QueuingStateNode(inner)
{
    internal double VirtualExecutionTime { get; set; }
}
