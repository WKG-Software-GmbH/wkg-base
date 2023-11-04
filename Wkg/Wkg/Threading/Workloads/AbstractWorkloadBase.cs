using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

/// <summary>
/// Represents an asynchronous workload that can be scheduled for execution.
/// </summary>
[DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
public abstract class AbstractWorkloadBase
{
    private protected uint _status;

    private protected AbstractWorkloadBase(WorkloadStatus status)
    {
        _status = status;
    }

    /// <summary>
    /// Gets the unique identifier of this workload.
    /// </summary>
    public ulong Id { get; } = WorkloadIdGenerator.Generate();

    /// <summary>
    /// Retrives the current <see cref="WorkloadStatus"/> of this workload.
    /// </summary>
    public WorkloadStatus Status => Volatile.Read(ref _status);

    /// <summary>
    /// Indicates whether the workload is in any of the terminal states: <see cref="WorkloadStatus.RanToCompletion"/>, <see cref="WorkloadStatus.Faulted"/>, or <see cref="WorkloadStatus.Canceled"/>.
    /// </summary>
    public virtual bool IsCompleted => Status.IsOneOf(CommonFlags.Completed);

    internal virtual void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) => Pass();

    /// <summary>
    /// Attempts to execute the action associated with this workload.
    /// </summary>
    /// <returns><see langword="true"/> if the workload was executed and is in any of the terminal states; <see langword="false"/> if the workload could not be executed.</returns>
    internal abstract bool TryRunSynchronously();

    /// <summary>
    /// Marks the workload as finalized before it falls out of scope and executes any Task continuations that were scheduled for it.
    /// Also allows the workload to be returned to a pool, if applicable.
    /// </summary>
    internal abstract void InternalRunContinuations();

    internal abstract void InternalAbort();

    /// <summary>
    /// Attempts to bind the workload to the specified qdisc.
    /// </summary>
    /// <remarks>
    /// The workload should be bound first, before being enqueued into the qdisc.
    /// </remarks>
    /// <param name="qdisc">The qdisc to bind to.</param>
    /// <returns><see langword="true"/> if the workload was successfully bound to the qdisc; <see langword="false"/> if the workload has already completed, is in an unbindable state, or another binding operation was faster.</returns>
    internal abstract bool TryInternalBindQdisc(IQdisc qdisc);

    /// <inheritdoc/>
    public override string ToString() => $"{GetType().Name} {Id} ({Status})";

    private protected sealed class QdiscCompletionSentinel : IQdisc
    {
        private const string _message = "Internal error: Qdisc completion sentinel should never be accessed. This is a bug. Please report this issue.";
        bool IQdisc.IsEmpty => ThrowHelper<bool>();
        int IQdisc.Count => ThrowHelper<int>();
        bool IQdisc.TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => (workload = null) == null && ThrowHelper<bool>();
        bool IQdisc.TryRemoveInternal(AwaitableWorkload workload) => ThrowHelper<bool>();
        void IQdisc.InternalInitialize(INotifyWorkScheduled parentScheduler) => ThrowHelper<bool>();
        void IQdisc.Complete() => ThrowHelper<bool>();

        [DoesNotReturn]
        [StackTraceHidden]
        private static T ThrowHelper<T>() => throw new WorkloadSchedulingException(_message);
    }
}

file static class WorkloadIdGenerator
{
    private static ulong _nextId;

    // this is a simple, fast, and thread-safe way to generate unique IDs.
    // it is possible for the IDs to be reused, but only after 2^64 IDs have been generated
    // and it is highly unlikely that someone keeps a workload alive for so long
    // that there are any collisions
    public static ulong Generate() => Interlocked.Increment(ref _nextId);
}