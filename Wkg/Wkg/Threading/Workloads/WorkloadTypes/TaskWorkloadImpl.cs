namespace Wkg.Threading.Workloads.WorkloadTypes;

// TODO: more Task types
internal class TaskWorkloadImpl(Func<CancellationFlag, Task> _action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload(status, options, cancellationToken)
{
    public TaskWorkloadImpl(Func<CancellationFlag, Task> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    private protected override Task ExecuteCoreAsync() => _action.Invoke(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
