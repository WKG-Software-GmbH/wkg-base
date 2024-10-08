﻿using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Configuration;

namespace Wkg.Threading.Workloads.Queuing;

/// <summary>
/// Represents any queuing discipline (qdisc) used to schedule workloads for execution.
/// </summary>
public interface IQdisc : IDisposable
{
    /// <summary>
    /// Returns a string representation of the qdisc and all child qdiscs.
    /// </summary>
    string ToTreeString();

    /// <summary>
    /// Initializes the qdisc with the specified parent scheduler.
    /// </summary>
    /// <param name="parentScheduler">The parent scheduler.</param>
    internal void InternalInitialize(INotifyWorkScheduled parentScheduler);

    /// <summary>
    /// Completes the qdisc, preventing any further workloads from being enqueued.
    /// </summary>
    internal void Complete();

    /// <summary>
    /// Determines whether any workloads are available for processing in this or any child qdisc. False negatives are possible, but not false positives.
    /// </summary>
    /// <remarks>
    /// If this property returns <see langword="true"/>, the underlying qdisc must be really be empty (strong guarantee).<br></br>
    /// If this property returns <see langword="false"/>, the underlying qdisc may or may not be empty (weak guarantee).<br></br>
    /// This means that scheduling attempts are blocked during evaluation, but dequeue operations may still be performed.
    /// </remarks>
    bool IsEmpty { get; }

    /// <summary>
    /// Provides an estimate of the total number of workloads in this qdisc and all child qdiscs.
    /// </summary>
    /// <remarks>
    /// This property is used to determine the best-effort count of the qdisc. As such, it is not guaranteed to be accurate. However, it is guaranteed to only return <c>0</c> if the qdisc is truly empty.
    /// </remarks>
    int BestEffortCount { get; }

    /// <summary>
    /// Attempts to dequeue a workload from this qdisc.
    /// </summary>
    /// <param name="workerId">The unique ID of the worker thread attempting to dequeue a workload. This ID is guaranteed to be in the range [0, <see cref="IQdiscBuilderContext.MaximumConcurrency"/>).</param>
    /// <param name="backTrack">Whether to repeat the same dequeue operation performed in the previous call to this method, according to the internal state of the qdisc. Instructs the implementing qdisc to back-track to the previous state if possible.</param>
    /// <param name="workload">The dequeued workload, if any.</param>
    /// <returns><see langword="true"/> if a workload was dequeued; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// The <paramref name="backTrack"/> parameter is set to <see langword="true"/> when execution of the previously dequeued workload was skipped due to cancellation, or any other valid reason to repeat the previous dequeue operation. For example, if a stale workload was dequeued and skipped in a round-robin qdisc, the next dequeue operation should repeat the same dequeue operation and assume the previous dequeue operation never happened. This is to ensure fairness in cases where workloads *should* have been removed from the qdisc previously due to cancellation, but could not be removed due to the qdisc not supporting removal of workloads. In such cases, the qdisc should repeat the dequeue operation until it finds a valid workload to execute.
    /// </remarks>
    internal bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    internal bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload);

    /// <summary>
    /// Notifies the qdisc that a worker thread has been terminated.
    /// </summary>
    /// <param name="workerId">The unique ID of the worker thread that was terminated.</param>
    /// <remarks>
    /// This method is called by the parent workload scheduler when a worker thread is terminated. 
    /// The qdisc should use this method to remove any references to the worker thread from its internal state.
    /// Classful qdiscs should forward this method to all child qdiscs.
    /// </remarks>
    public void OnWorkerTerminated(int workerId);

    /// <summary>
    /// Attempts to remove the specified workload from this qdisc.
    /// </summary>
    /// <param name="workload">The workload to remove.</param>
    /// <returns><see langword="true"/> if the workload was removed; <see langword="false"/> if the workload could not be found or the qdisc does not support removal of workloads.</returns>
    internal bool TryRemoveInternal(AwaitableWorkload workload);

    // TODO: add a clear method to remove all workloads from the qdisc and be able to reschedule them (e.g. when a child qdisc is removed)
}

/// <summary>
/// A qdisc that can be uniquely identified by a handle.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
public interface IQdisc<THandle> : IQdisc where THandle : unmanaged
{
    /// <summary>
    /// A handle uniquely identifying this qdisc.
    /// </summary>
    public ref readonly THandle Handle { get; }
}