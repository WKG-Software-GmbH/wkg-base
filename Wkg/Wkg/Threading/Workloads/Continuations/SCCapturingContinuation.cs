namespace Wkg.Threading.Workloads.Continuations;

internal class SCCapturingContinuation(IWorkloadContinuation innerContinuation, SynchronizationContext _synchronizationContext, bool flowExecutionContext) 
    : ECContinuationBase(innerContinuation, flowExecutionContext)
{
    protected override void PostContinuation(Action<object?> callback, object? state) =>
        _synchronizationContext.Post(new SendOrPostCallback(callback), state);
}
