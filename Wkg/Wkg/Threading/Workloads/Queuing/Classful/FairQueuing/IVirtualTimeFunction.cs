using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

public interface IVirtualTimeFunction
{
    double CalculateVirtualExecutionTime(GfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo);

    double CalculateVirtualFinishTime(GfqWeight weight, IVirtualTimeTable timeTable, double virtualExecutionTime, double lastVirtualFinishTime);

    double CalculateVirtualAccumulatedFinishTime(GfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo, double lastVirtualFinishTime);
}

internal delegate double VirtualExecutionTimeFunction(GfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo);
internal delegate double VirtualFinishTimeFunction(GfqWeight weight, IVirtualTimeTable timeTable, double virtualExecutionTime, double lastVirtualFinishTime);
internal delegate double VirtualAccumulatedFinishTimeFunction(GfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo, double lastVirtualFinishTime);