using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

public interface IVirtualTimeFunction
{
    double CalculateVirtualExecutionTime(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo);

    double CalculateVirtualFinishTime(WfqWeight weight, IVirtualTimeTable timeTable, double virtualExecutionTime, double lastVirtualFinishTime);

    double CalculateVirtualAccumulatedFinishTime(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo, double lastVirtualFinishTime);
}

internal delegate double VirtualExecutionTimeFunction(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo);
internal delegate double VirtualFinishTimeFunction(WfqWeight weight, IVirtualTimeTable timeTable, double virtualExecutionTime, double lastVirtualFinishTime);
internal delegate double VirtualAccumulatedFinishTimeFunction(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo, double lastVirtualFinishTime);