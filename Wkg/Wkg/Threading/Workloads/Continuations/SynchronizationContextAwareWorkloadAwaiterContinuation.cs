using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads.Continuations;

internal class SynchronizationContextAwareWorkloadAwaiterContinuation : WorkloadAwaiterContinuation
{
    private readonly SynchronizationContext _synchronizationContext;

    public SynchronizationContextAwareWorkloadAwaiterContinuation(Action continuation, SynchronizationContext synchronizationContext, bool flowExecutionContext)
        : base(continuation, flowExecutionContext)
    {
        _synchronizationContext = synchronizationContext;
    }

    public override void Invoke(AwaitableWorkload workload)
    {
        if (_capturedContext is null)
        {
            DebugLog.WriteDiagnostic("Posting SCCallback to synchronization context", LogWriter.Blocking);
            _synchronizationContext.Post(SCCallback, this);
        }
        else
        {
            DebugLog.WriteDiagnostic("Posting ECCallback to synchronization context", LogWriter.Blocking);
            _synchronizationContext.Post(ECCallback, this);
        }
    }

    private static void SCCallback(object? context)
    {
        SynchronizationContextAwareWorkloadAwaiterContinuation self = ReinterpretCast<SynchronizationContextAwareWorkloadAwaiterContinuation>(context)!;
        DebugLog.WriteDiagnostic("Invoking continuation...", LogWriter.Blocking);
        self._continuation();
    }

    private static void ECCallback(object? context)
    {
        SynchronizationContextAwareWorkloadAwaiterContinuation self = ReinterpretCast<SynchronizationContextAwareWorkloadAwaiterContinuation>(context)!;
        DebugLog.WriteDiagnostic("Restoring execution context...", LogWriter.Blocking);
        ExecutionContext.Run(self._capturedContext!, SCCallback, self);
    }
}
