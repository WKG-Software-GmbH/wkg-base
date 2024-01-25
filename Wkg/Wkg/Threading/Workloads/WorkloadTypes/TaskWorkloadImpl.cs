namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class TaskWorkloadImpl(Func<CancellationFlag, Task> _task, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload(status, options, cancellationToken)
{
    public TaskWorkloadImpl(Func<CancellationFlag, Task> task, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(task, WorkloadStatus.Created, options, cancellationToken) => Pass();

    private protected override Task ExecuteCoreAsync() => _task.Invoke(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _task.Method.MethodHandle.GetFunctionPointer();
}
