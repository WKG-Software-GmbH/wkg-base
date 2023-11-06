namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImpl : Workload
{
    private readonly Action<CancellationFlag> _action;

    internal WorkloadImpl(Action<CancellationFlag> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImpl(Action<CancellationFlag> action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _action = action;
    }

    private protected override void ExecuteCore() => _action(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
