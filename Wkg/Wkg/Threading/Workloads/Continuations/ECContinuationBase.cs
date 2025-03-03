using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wkg.Threading.Workloads.Continuations;

internal abstract class ECContinuationBase : IWorkloadContinuation
{
    private readonly ExecutionContext? _capturedContext;
    private readonly IWorkloadContinuation _innerContinuation;

    protected ECContinuationBase(IWorkloadContinuation innerContinuation, bool flowExecutionContext)
    {
        _innerContinuation = innerContinuation;
        if (flowExecutionContext)
        {
            _capturedContext = ExecutionContext.Capture();
        }
    }

    public void Invoke(AbstractWorkloadBase workload)
    {
        if (_capturedContext is null)
        {
            using (ExecutionContext.SuppressFlow())
            {
                PostContinuation(InvokeContinuation, workload);
            }
        }
        else
        {
            PostContinuation(RunWithCapturedEC, workload);
        }
    }

    public void InvokeInline(AbstractWorkloadBase workload) => _innerContinuation.InvokeInline(workload);

    private void RunWithCapturedEC(object? state) => ExecutionContext.Run(_capturedContext!, InvokeContinuation, state);

    protected abstract void PostContinuation(Action<object?> callback, object? state);

    protected void InvokeContinuation(object? workload)
    {
        Debug.Assert(workload is AbstractWorkloadBase);
        _innerContinuation.Invoke(Unsafe.As<AbstractWorkloadBase>(workload));
    }
}
