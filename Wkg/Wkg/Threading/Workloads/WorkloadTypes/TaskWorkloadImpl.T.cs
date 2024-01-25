namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class TaskWorkloadImpl<TResult>(Func<CancellationFlag, Task<TResult>> _task, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload<TResult>(status, options, cancellationToken)
{
    public TaskWorkloadImpl(Func<CancellationFlag, Task<TResult>> task, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(task, WorkloadStatus.Created, options, cancellationToken) => Pass();

    private protected override Task<TResult> ExecuteCoreAsync() => _task.Invoke(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _task.Method.MethodHandle.GetFunctionPointer();
}
