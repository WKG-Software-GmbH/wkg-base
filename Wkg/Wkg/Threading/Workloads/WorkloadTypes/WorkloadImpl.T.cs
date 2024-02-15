namespace Wkg.Threading.Workloads;

internal class WorkloadImpl<TResult> : Workload<TResult>
{
    private readonly Func<CancellationFlag, TResult> _func;

    internal WorkloadImpl(Func<CancellationFlag, TResult> func, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(func, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal WorkloadImpl(Func<CancellationFlag, TResult> func, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _func = func;
    }

    private protected override TResult ExecuteCore() => _func(new CancellationFlag(this));

    internal override nint GetPayloadFunctionPointer() => _func.Method.MethodHandle.GetFunctionPointer();
}
