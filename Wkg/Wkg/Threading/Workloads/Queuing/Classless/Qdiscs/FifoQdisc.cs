using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Wkg.Common.ThrowHelpers;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

internal class FifoQdisc<THandle> : Qdisc<THandle>, IClasslessQdisc<THandle, FifoQdisc<THandle>> where THandle : unmanaged
{
    private readonly ConcurrentQueue<AbstractWorkloadBase> _queue;

    internal FifoQdisc(THandle handle) : base(handle)
    {
        _queue = new ConcurrentQueue<AbstractWorkloadBase>();
    }

    public static FifoQdisc<THandle> Create(THandle handle)
    {
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);
        return new FifoQdisc<THandle>(handle);
    }

    public override bool IsEmpty => _queue.IsEmpty;

    public override int Count => _queue.Count;

    public void Enqueue(AbstractWorkloadBase workload)
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

    protected override bool TryDequeueInternal(bool backTrack, [NotNullWhen(true)] out AbstractWorkloadBase? workload) => _queue.TryDequeue(out workload);

    protected override bool TryRemoveInternal(CancelableWorkload workload) => false;
}
