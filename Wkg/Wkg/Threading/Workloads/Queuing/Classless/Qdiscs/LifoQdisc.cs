using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

internal class LifoQdisc<THandle> : Qdisc<THandle>, IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly ConcurrentStack<Workload> _stack;

    public LifoQdisc(THandle handle) : base(handle)
    {
        _stack = new ConcurrentStack<Workload>();
    }

    public override bool IsEmpty => _stack.IsEmpty;

    public override int Count => _stack.Count;

    public void Enqueue(Workload workload)
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

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out Workload? workload) => _stack.TryPop(out workload);

    protected override bool TryRemoveInternal(Workload workload) => false;
}
