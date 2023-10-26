using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

internal class FifoQdisc<THandle> : Qdisc<THandle>, IClasslessQdisc<THandle> where THandle : unmanaged
{
    private readonly ConcurrentQueue<Workload> _queue;

    public FifoQdisc(THandle handle) : base(handle)
    {
        _queue = new ConcurrentQueue<Workload>();
    }

    public override bool IsEmpty => _queue.IsEmpty;

    public override int Count => _queue.Count;

    public void Enqueue(Workload workload)
    {
        INotifyWorkScheduled parentScheduler = ParentScheduler;
        _queue.Enqueue(workload);
        if (workload.TryInternalBindQdisc(this))
        {
            parentScheduler.OnWorkScheduled();
        }
        else
        {
            DebugLog.WriteWarning("A workload was scheduled, but could not be bound to the qdisc. This is a likely a bug in the qdisc scheduler implementation.", LogWriter.Blocking);
        }
    }

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out Workload? workload) => _queue.TryDequeue(out workload);

    protected override bool TryRemoveInternal(Workload workload) => false;
}
