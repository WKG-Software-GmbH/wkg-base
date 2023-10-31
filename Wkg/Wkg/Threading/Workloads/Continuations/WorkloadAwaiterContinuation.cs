using Wkg.Common.Extensions;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Unmanaged;

namespace Wkg.Threading.Workloads.Continuations;

internal class WorkloadAwaiterContinuation : WorkloadContinuationBase
{
    private protected readonly ExecutionContext? _capturedContext;

    public WorkloadAwaiterContinuation(Action continuation, bool flowExecutionContext) : base(continuation)
    {
        if (flowExecutionContext)
        {
            _capturedContext = ExecutionContext.Capture();
        }
    }

    public override void Invoke(AwaitableWorkload workload)
    {
        if (_capturedContext is null)
        {
            DebugLog.WriteDiagnostic("Queueing await continuation to thread pool", LogWriter.Blocking);
            ThreadPool.QueueUserWorkItem(TPCallback, this);
        }
        else
        {
            DebugLog.WriteDiagnostic("Posting await continuation to execution context", LogWriter.Blocking);
            ThreadPool.QueueUserWorkItem(ECCallbackWrapper, this);
        }
    }

    private static void TPCallback(object? state)
    {
        DebugLog.WriteDiagnostic("Invoking await continuation from thread pool", LogWriter.Blocking);
        ReinterpretCast<WorkloadAwaiterContinuation>(state)!._continuation();
    }

    private static void ECCallbackWrapper(object? state)
    {
        DebugLog.WriteDiagnostic("Restoring execution context for await continuation", LogWriter.Blocking);
        WorkloadAwaiterContinuation self = ReinterpretCast<WorkloadAwaiterContinuation>(state)!;
        ExecutionContext.Run(self._capturedContext!, TPCallback, self);
    }
}
