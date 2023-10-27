using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

internal class LifoQdisc<THandle> : Qdisc<THandle>, IClasslessQdisc<THandle, LifoQdisc<THandle>> where THandle : unmanaged
{
    private readonly ConcurrentStack<AbstractWorkloadBase> _stack;

    internal LifoQdisc(THandle handle) : base(handle)
    {
        _stack = new ConcurrentStack<AbstractWorkloadBase>();
    }

    public static LifoQdisc<THandle> Create(THandle handle)
    {
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);
        return new LifoQdisc<THandle>(handle);
    }

    public override bool IsEmpty => _stack.IsEmpty;

    public override int Count => _stack.Count;

    public void Enqueue(AbstractWorkloadBase workload)
    {
        INotifyWorkScheduled parentScheduler = ParentScheduler;
        _stack.Push(workload);
        if (workload.TryInternalBindQdisc(this))
        {
            parentScheduler.OnWorkScheduled();
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _stack.TryPop(out workload);

    protected override bool TryRemoveInternal(CancelableWorkload workload) => false;
}
