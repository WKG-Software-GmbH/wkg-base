using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.Random;

internal class WorkStealingQdisc<THandle>(THandle handle, Predicate<object?>? predicate) : ClasslessQdisc<THandle>(handle, predicate) where THandle : unmanaged
{
    private readonly ConcurrentBag<AbstractWorkloadBase> _queue = [];

    public override bool IsEmpty => _queue.IsEmpty;

    public override int BestEffortCount => _queue.Count;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

    protected override void EnqueueDirectLocal(AbstractWorkloadBase workload) => _queue.Add(workload);

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryTake(out workload);

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

    public override string ToString() => $"WorkStealing qdisc (handle: {Handle}, count: {BestEffortCount})";
}
