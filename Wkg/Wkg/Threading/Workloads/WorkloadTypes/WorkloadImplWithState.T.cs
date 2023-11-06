namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImplWithState<TState, TResult> : Workload<TResult>
{
    private readonly TState _state;
    private readonly Func<TState, CancellationFlag, TResult> _func;

    internal WorkloadImplWithState(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, func, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithState(TState state, Func<TState, CancellationFlag, TResult> func, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _func = func;
        _state = state;
    }

    private protected override TResult ExecuteCore() => _func(_state, new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _func.Method.MethodHandle.GetFunctionPointer();
}
