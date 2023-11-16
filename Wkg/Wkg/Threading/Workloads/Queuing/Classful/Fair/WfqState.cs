using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.Fair;

internal class WfqState(QueuingStateNode? inner, EventuallyConsistentVirtualTimeTableEntry timingInfo, WfqWeight qdiscWeight) : QueuingStateNode(inner)
{
    public EventuallyConsistentVirtualTimeTableEntry TimingInfo { get; } = timingInfo;

    public WfqWeight QdiscWeight { get; } = qdiscWeight;
}
