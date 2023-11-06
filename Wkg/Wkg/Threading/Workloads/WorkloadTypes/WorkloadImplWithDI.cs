using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImplWithDI : Workload
{
    private readonly Action<IWorkloadServiceProvider, CancellationFlag> _action;
    private IWorkloadServiceProvider? _serviceProvider;

    internal WorkloadImplWithDI(Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(action, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithDI(Action<IWorkloadServiceProvider, CancellationFlag> action, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _action = action;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    private protected override void ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        _action(_serviceProvider!, new CancellationFlag(this));
    }

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
