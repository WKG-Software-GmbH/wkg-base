using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads;

internal class WorkloadImplWithDIAndState<TState> : Workload
{
    private readonly Action<TState, IWorkloadServiceProvider, CancellationFlag> _action;
    private readonly TState _state;
    private IWorkloadServiceProvider? _serviceProvider;

    internal WorkloadImplWithDIAndState(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithDIAndState(TState state, Action<TState, IWorkloadServiceProvider, CancellationFlag> action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _action = action;
        _state = state;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) => 
        _serviceProvider = serviceProvider;

    private protected override void ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        _action(_state, _serviceProvider!, new CancellationFlag(this));
    }
}