namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class TaskWorkloadImplWithState<TState, TResult>(TState _state, Func<TState, CancellationFlag, Task<TResult>> _task, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload<TResult>(status, options, cancellationToken)
{
    public TaskWorkloadImplWithState(TState state, Func<TState, CancellationFlag, Task<TResult>> task, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, task, WorkloadStatus.Created, options, cancellationToken) => Pass();

    private protected override Task<TResult> ExecuteCoreAsync() => _task.Invoke(_state, new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _task.Method.MethodHandle.GetFunctionPointer();
}
