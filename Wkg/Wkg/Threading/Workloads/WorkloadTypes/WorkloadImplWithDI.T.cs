using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class WorkloadImplWithDI<TResult> : Workload<TResult>
{
    private readonly Func<IWorkloadServiceProvider, CancellationFlag, TResult> _func;
    private IWorkloadServiceProvider? _serviceProvider;

    internal WorkloadImplWithDI(Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(func, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImplWithDI(Func<IWorkloadServiceProvider, CancellationFlag, TResult> func, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _func = func;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    private protected override TResult ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        return _func(_serviceProvider!, new CancellationFlag(this));
    }

    internal override nint GetPayloadFunctionPointer() => _func.Method.MethodHandle.GetFunctionPointer();
}
