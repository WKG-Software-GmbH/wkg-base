using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads.Factories;

public abstract class WorkloadFactory<THandle> : IDisposable where THandle : unmanaged
{
    private IClasslessQdisc<THandle> _root;

    private protected AnonymousWorkloadPoolManager? Pool { get; }

    public WorkloadContextOptions DefaultOptions { get; }

    private protected WorkloadFactory(IClasslessQdisc<THandle> root, AnonymousWorkloadPoolManager? pool, WorkloadContextOptions? options)
    {
        _root = root;
        Pool = pool;
        DefaultOptions = options ?? new WorkloadContextOptions();
    }

    [MemberNotNullWhen(true, nameof(Pool))]
    private protected bool SupportsPooling => Pool is not null;

    private protected ref IClasslessQdisc<THandle> RootRef
    {
        get
        {
            _root.AssertNotCompleted();
            return ref _root;
        }
    }

    public void Dispose()
    {
        if (_root.IsCompleted)
        {
            return;
        }
        DebugLog.WriteInfo("Workload factory: disposal requested.", LogWriter.Blocking);
        // store the current root scheduler.
        INotifyWorkScheduled scheduler = _root.ParentScheduler;
        // CAS in the completion sentinel to prevent further scheduling.
        _root.Complete();
        // dispose the root scheduler and wait for all workers to exit.
        scheduler.DisposeRoot();
        // clear all workloads from the root scheduler.
        ObjectDisposedException exception = new(nameof(WorkloadFactory<THandle>), "The parent workload factory was disposed.");
        ExceptionDispatchInfo.SetCurrentStackTrace(exception);
        while (_root.TryDequeueInternal(workerId: 0, backTrack: false, out AbstractWorkloadBase? workload))
        {
            if (!workload.IsCompleted)
            {
                workload.InternalAbort(exception);
                DebugLog.WriteWarning($"Disposing workload factory but scheduler still contains uncompleted workloads. Forcefully aborted workload {workload}.", LogWriter.Blocking);
            }
            if (workload is AwaitableWorkload awaitable)
            {
                awaitable.UnbindQdiscUnsafe();
            }
        }
        // dispose the qdisc data structures.
        DebugLog.WriteDebug("Disposing scheduler data structures NOW.", LogWriter.Blocking);
        _root.Dispose();
    }
}
