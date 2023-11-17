using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.Fifo;

/// <summary>
/// A qdisc that implements the First-In-First-Out (FIFO) scheduling algorithm.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
/// <param name="handle">The handle of the qdisc.</param>
/// <param name="predicate">The predicate used to determine if a workload can be scheduled.</param>
internal sealed class FifoQdisc<THandle>(THandle handle, Predicate<object?>? predicate) : ClasslessQdisc<THandle>(handle, predicate), IClassifyingQdisc<THandle> where THandle : unmanaged
{
    private readonly ConcurrentQueue<AbstractWorkloadBase> _queue = new();

    public override bool IsEmpty => _queue.IsEmpty;

    public override int Count => _queue.Count;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

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

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryDequeue(out workload);

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload) => TryEnqueueDirect(state, workload);

    protected override bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload) => false;

    protected override bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload)
    {
        if (Predicate.Invoke(state))
        {
            EnqueueDirect(workload);
            return true;
        }
        return false;
    }
    protected override bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path) => false;

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryPeek(out workload);

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;
}
