using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads;

internal class WorkloadImplWithDIAndState<TState, TResult> : Workload<TResult>
{
    private readonly Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> _func;
    private readonly TState _state;
    private IWorkloadServiceProvider? _serviceProvider;

    internal WorkloadImplWithDIAndState(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, func, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithDIAndState(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _func = func;
        _state = state;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) => 
        _serviceProvider = serviceProvider;

    private protected override TResult ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        return _func(_state, _serviceProvider!, new CancellationFlag(this));
    }
}