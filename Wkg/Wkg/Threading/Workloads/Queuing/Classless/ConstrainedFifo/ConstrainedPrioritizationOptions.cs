namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

/// <summary>
/// Prioritization options for space-constrained queuing disciplines.
/// </summary>
public enum ConstrainedPrioritizationOptions
{
    /// <summary>
    /// Prioritize worker threads over scheduling threads to minimize the execution time of already scheduled workloads.
    /// This will prioritize dequeuing workloads over enqueuing new ones, which will minimize the amount of time spent waiting for workloads to complete.
    /// May increase throughput at the cost of responsiveness.
    /// </summary>
    MinimizeWorkloadCancellation,
    /// <summary>
    /// Prioritize scheduling threads over worker threads to minimize the amount of time spent waiting for workloads to be scheduled.
    /// On average, this may result in more workloads being cancelled, prioritizing enqueuing new workloads over dequeuing existing ones.
    /// May increase resposiveness at the cost of throughput.
    /// </summary>
    MinimizeSchedulingDelay
}