namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImplWithState<TState> : Workload
{
    private readonly TState _state;
    private readonly Action<TState, CancellationFlag> _action;

    internal WorkloadImplWithState(TState state, Action<TState, CancellationFlag> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithState(TState state, Action<TState, CancellationFlag> action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _action = action;
        _state = state;
    }

    private protected override void ExecuteCore() => _action(_state, new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
