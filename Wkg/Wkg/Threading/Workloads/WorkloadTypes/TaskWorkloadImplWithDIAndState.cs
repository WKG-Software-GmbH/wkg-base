using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class TaskWorkloadImplWithDIAndState<TState>(TState _state, Func<TState, IWorkloadServiceProvider, CancellationFlag, Task> _task, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload(status, options, cancellationToken)
{
    private IWorkloadServiceProvider? _serviceProvider;

    public TaskWorkloadImplWithDIAndState(TState state, Func<TState, IWorkloadServiceProvider, CancellationFlag, Task> task, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(state, task, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) => 
        _serviceProvider = serviceProvider;

    private protected override Task ExecuteCoreAsync()
    {
        Debug.Assert(_serviceProvider is not null);
        return _task.Invoke(_state, _serviceProvider, new CancellationFlag(this));
    }

    internal override nint GetPayloadFunctionPointer() => _task.Method.MethodHandle.GetFunctionPointer();
}
