using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads.Queuing;

public interface IQdisc
{
    void InternalInitialize(INotifyWorkScheduled parentScheduler);

    /// <summary>
    /// Determines whether any workloads are available for processing in this or any child qdisc.
    /// </summary>
    /// <remarks>
    /// Implementations must ensure that this property is thread-safe.
    /// </remarks>
    bool IsEmpty { get; }

    /// <summary>
    /// Attempts to dequeue a workload from this qdisc.
    /// </summary>
    /// <param name="backTrack">Whether to repeat the same dequeue operation performed in the previous call to this method, according to the internal state of the qdisc. Instructs the implementing qdisc to back-track to the previous state if possible.</param>
    /// <param name="workload">The dequeued workload, if any.</param>
    /// <returns><see langword="true"/> if a workload was dequeued; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The <paramref name="backTrack"/> parameter is set to <see langword="true"/> when execution of the previously dequeued workload was skipped due to cancellation, or any other valid reason to repeat the previous dequeue operation. For example, if a stale workload was dequeued and skipped in a round-robin qdisc, the next dequeue operation should repeat the same dequeue operation and assume the previous dequeue operation never happened. This is to ensure fairness in cases where workloads *should* have been removed from the qdisc previously due to cancellation, but could not be removed due to the qdisc not supporting removal of workloads. In such cases, the qdisc should repeat the dequeue operation until it finds a valid workload to execute.
    /// </remarks>
    bool TryDequeue(bool backTrack, [NotNullWhen(true)] out Workload? workload);

    /// <summary>
    /// Attempts to remove the specified workload from this qdisc.
    /// </summary>
    /// <param name="workload">The workload to remove.</param>
    /// <returns><see langword="true"/> if the workload was removed; <see langword="false"/> if the workload could not be found or the qdisc does not support removal of workloads.</returns>
    bool TryRemove(Workload workload);
}

public interface IClasslessQdisc : IQdisc
{
    void Enqueue(Workload workload);
}

public interface IClassfulQdisc : IClasslessQdisc, INotifyWorkScheduled
{
    void AddChild(IClasslessQdisc child);

    void RemoveChild(IClasslessQdisc child);
}
