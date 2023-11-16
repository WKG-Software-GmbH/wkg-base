using Wkg.Threading.Workloads.Queuing.VirtualTime;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

internal sealed class ParameterizedWfqVirtualTimeFunction(WfqSchedulingParams schedulingParams) : IVirtualTimeFunction
{
    private readonly PreferredFairness _preferredFairness = schedulingParams.PreferredFairness;
    private readonly VirtualTimeModel _schedulerTimeModel = schedulingParams.SchedulerTimeModel;
    private readonly VirtualTimeModel _executionTimeModel = schedulingParams.ExecutionTimeModel;

    public double CalculateVirtualAccumulatedFinishTime(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo, double lastVirtualFinishTime)
    {
        double virtualBaseTime = _preferredFairness == PreferredFairness.ShortTerm
            ? timeTable.Now()
            : lastVirtualFinishTime;
        double assumedExecutionTime = _executionTimeModel switch
        {
            VirtualTimeModel.BestCase => timingInfo.BestCaseAverageExecutionTime,
            VirtualTimeModel.WorstCase => timingInfo.WorstCaseAverageExecutionTime,
            VirtualTimeModel.Average or _ => timingInfo.AverageExecutionTime,
        };
        return virtualBaseTime + assumedExecutionTime * weight.ExecutionPunishmentFactor;
    }

    public double CalculateVirtualExecutionTime(WfqWeight weight, IVirtualTimeTable timeTable, EventuallyConsistentVirtualTimeTableEntry timingInfo) => _schedulerTimeModel switch
    {
        VirtualTimeModel.BestCase => timingInfo.BestCaseAverageExecutionTime,
        VirtualTimeModel.WorstCase => timingInfo.WorstCaseAverageExecutionTime,
        VirtualTimeModel.Average or _ => timingInfo.AverageExecutionTime,
    } * weight.WorkloadSchedulingWeight;

    public double CalculateVirtualFinishTime(WfqWeight weight, IVirtualTimeTable timeTable, double virtualExecutionTime, double lastVirtualFinishTime) =>
        lastVirtualFinishTime + virtualExecutionTime;
}
