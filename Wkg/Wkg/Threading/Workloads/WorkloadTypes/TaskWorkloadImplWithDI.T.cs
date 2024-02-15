using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class TaskWorkloadImplWithDI<TResult>(Func<IWorkloadServiceProvider, CancellationFlag, Task<TResult>> _task, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken) 
    : TaskWorkload<TResult>(status, options, cancellationToken)
{
    private IWorkloadServiceProvider? _serviceProvider;

    public TaskWorkloadImplWithDI(Func<IWorkloadServiceProvider, CancellationFlag, Task<TResult>> task, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(task, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    private protected override Task<TResult> ExecuteCoreAsync()
    {
        Debug.Assert(_serviceProvider is not null);
        return _task.Invoke(_serviceProvider, new CancellationFlag(this));
    }

    internal override nint GetPayloadFunctionPointer() => _task.Method.MethodHandle.GetFunctionPointer();
}
