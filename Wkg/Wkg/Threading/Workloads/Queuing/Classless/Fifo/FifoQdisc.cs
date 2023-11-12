using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Fifo;

/// <summary>
/// A qdisc that implements the First-In-First-Out (FIFO) scheduling algorithm.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class FifoQdisc<THandle>(THandle handle) : ClasslessQdisc<THandle>(handle), IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly ConcurrentQueue<AbstractWorkloadBase> _queue = new();

    /// <inheritdoc/>
    public override bool IsEmpty => _queue.IsEmpty;

    /// <inheritdoc/>
    public override int Count => _queue.Count;

    /// <inheritdoc/>
    protected override void EnqueueDirect(AbstractWorkloadBase workload)
    {
        if (TryBindWorkload(workload))
        {
            _queue.Enqueue(workload);
            NotifyWorkScheduled();
        }
        else if (workload.IsCompleted)
        {
            DebugLog.WriteWarning($"A workload was scheduled, but it was already completed. What are you doing?", LogWriter.Blocking);
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    /// <inheritdoc/>
    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryDequeue(out workload);

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryPeek(out workload);

    /// <inheritdoc/>
    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;
}
