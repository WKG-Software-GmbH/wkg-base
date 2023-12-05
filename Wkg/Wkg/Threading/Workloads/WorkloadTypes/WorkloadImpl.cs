namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImpl(Action<CancellationFlag> _action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : Workload(status, options, cancellationToken)
{
    public WorkloadImpl(Action<CancellationFlag> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    private protected override void ExecuteCore() => _action(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
