using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadImplWithDIAndState<TState> : AnonymousWorkload
{
    private readonly TState _state;
    private readonly Action<TState, IWorkloadServiceProvider> _action;
    private IWorkloadServiceProvider? _serviceProvider;

    internal AnonymousWorkloadImplWithDIAndState(TState state, Action<TState, IWorkloadServiceProvider> action) 
        : this(state, WorkloadStatus.Created, action) => Pass();

    internal AnonymousWorkloadImplWithDIAndState(TState state, WorkloadStatus status, Action<TState, IWorkloadServiceProvider> action) : base(status)
    {
        _state = state;
        _action = action;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    private protected override void ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        _action(_state, _serviceProvider!);
    }
}
