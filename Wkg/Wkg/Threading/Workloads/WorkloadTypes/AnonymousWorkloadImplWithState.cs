namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadImplWithState<TState> : AnonymousWorkload
{
    private readonly TState _state;
    private readonly Action<TState> _action;

    internal AnonymousWorkloadImplWithState(TState state, Action<TState> action) : this(state, WorkloadStatus.Created, action) => Pass();

    internal AnonymousWorkloadImplWithState(TState state, WorkloadStatus status, Action<TState> action) : base(status)
    {
        _state = state;
        _action = action;
    }

    private protected override void ExecuteCore() => _action(_state);
}
