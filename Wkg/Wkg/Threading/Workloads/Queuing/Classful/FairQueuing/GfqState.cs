using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

internal class GfqState(QueuingStateNode? inner, EventuallyConsistentVirtualTimeTableEntry timingInfo, GfqWeight qdiscWeight) : QueuingStateNode(inner)
{
    public EventuallyConsistentVirtualTimeTableEntry TimingInfo { get; } = timingInfo;

    public GfqWeight QdiscWeight { get; } = qdiscWeight;
}
