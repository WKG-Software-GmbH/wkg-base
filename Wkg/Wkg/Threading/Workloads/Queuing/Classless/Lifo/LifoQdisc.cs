using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless.Lifo;

/// <summary>
/// A qdisc that implements the Last-In-First-Out (LIFO) scheduling algorithm.
/// </summary>
/// <typeparam name="THandle">The type of the handle.</typeparam>
internal sealed class LifoQdisc<THandle>(THandle handle, Predicate<object?>? predicate) : ClasslessQdisc<THandle>(handle, predicate) where THandle : unmanaged
{
    private readonly ConcurrentStack<AbstractWorkloadBase> _stack = new();

    public override bool IsEmpty => _stack.IsEmpty;

    public override int BestEffortCount => _stack.Count;

    protected override bool CanClassify(object? state) => Predicate.Invoke(state);

    protected override bool ContainsChild(THandle handle) => false;

    protected override void EnqueueDirectLocal(AbstractWorkloadBase workload) => _stack.Push(workload);

    protected override bool TryDequeueInternal(int workerId, bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => 
        _stack.TryPop(out workload);

    protected override bool TryEnqueue(object? state, AbstractWorkloadBase workload) => 
        TryEnqueueDirect(state, workload);

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

    protected override bool TryPeekUnsafe(int workerId, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _stack.TryPeek(out workload);

    protected override bool TryRemoveInternal(AwaitableWorkload workload) => false;
}
