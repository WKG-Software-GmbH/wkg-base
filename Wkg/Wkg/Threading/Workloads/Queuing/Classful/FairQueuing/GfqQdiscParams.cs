using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

internal class GfqQdiscParams
{
    public Predicate<object?>? Predicate { get; set; }
    public required IClasslessQdiscBuilder? Inner { get; set; }
    public required int ConcurrencyLevel { get; set; }
    public required int ExpectedNumberOfDistinctPayloads { get; set; }
    public required int MeasurementSampleLimit { get; set; }
    public required bool PreferPreciseMeasurements { get; set; } = false;
    public PreferredFairness PreferredFairness { get; set; } = PreferredFairness.ShortTerm;
    public VirtualTimeModel SchedulerTimeModel { get; set; } = VirtualTimeModel.WorstCase;
    public VirtualTimeModel ExecutionTimeModel { get; set; } = VirtualTimeModel.Average;
    public VirtualFinishTimeFunction? VirtualFinishTimeFunction { get; set; }
    public VirtualExecutionTimeFunction? VirtualExecutionTimeFunction { get; set; }
    public VirtualAccumulatedFinishTimeFunction? VirtualAccumulatedFinishTimeFunction { get; set; }

    public bool HasVirtualTimeFunction => VirtualFinishTimeFunction is not null && VirtualExecutionTimeFunction is not null && VirtualAccumulatedFinishTimeFunction is not null;

    public required WfqSchedulingParams SchedulingParams
    {
        get => new(PreferredFairness, SchedulerTimeModel, ExecutionTimeModel);
        set => (PreferredFairness, SchedulerTimeModel, ExecutionTimeModel) = value;
    }
}

internal record WfqSchedulingParams(PreferredFairness PreferredFairness, VirtualTimeModel SchedulerTimeModel, VirtualTimeModel ExecutionTimeModel)
{
    public static WfqSchedulingParams Default { get; } = new(PreferredFairness.ShortTerm, VirtualTimeModel.WorstCase, VirtualTimeModel.Average);
}

/// <summary>
/// The preferred fairness model of a fair queue.
/// </summary>
public enum PreferredFairness
{
    /// <summary>
    /// Fairness will be determined purely based on the last time a payload was dequeued from a given queue.
    /// This prioritizes payloads from queues that have not been dequeued for a long time.
    /// </summary>
    ShortTerm,
    /// <summary>
    /// The total execution times of all payloads that have ever been dequeued from a given queue will be taken into
    /// account when determining fairness. This prioritizes payloads from queues that had a low total execution time
    /// in the past.
    /// </summary>
    LongTerm
}

/// <summary>
/// The virtual time model of a fair queue.
/// </summary>
public enum VirtualTimeModel
{
    /// <summary>
    /// The virtual execution time of a payload is determined by the average execution time of all invocations of the
    /// underlying delegate.
    /// </summary>
    Average,
    /// <summary>
    /// The virtual execution time of a payload is determined by the average best-case execution time of all invocations
    /// of the underlying delegate. Best-case execution time is defined as lower-than-average execution time within a
    /// given degree of confidence.
    /// </summary>
    BestCase,
    /// <summary>
    /// The virtual execution time of a payload is determined by the average worst-case execution time of all invocations
    /// of the underlying delegate. Worst-case execution time is defined as higher-than-average execution time within a
    /// given degree of confidence.
    /// </summary>
    WorstCase
}