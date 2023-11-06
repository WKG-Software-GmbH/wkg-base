using System.Diagnostics;
using Wkg.Data.Pooling;
using Wkg.Threading.Workloads.DependencyInjection;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadImplWithDI : AnonymousWorkload, IPoolable<AnonymousWorkloadImplWithDI>
{
    private readonly IPool<AnonymousWorkloadImplWithDI>? _pool;
    private Action<IWorkloadServiceProvider> _action;
    private IWorkloadServiceProvider? _serviceProvider;

    private AnonymousWorkloadImplWithDI(IPool<AnonymousWorkloadImplWithDI> pool) : this(WorkloadStatus.Created, null!)
    {
        _pool = pool;
    }

    public static AnonymousWorkloadImplWithDI Create(IPool<AnonymousWorkloadImplWithDI> pool) =>
        new(pool);

    internal AnonymousWorkloadImplWithDI(Action<IWorkloadServiceProvider> action) : this(WorkloadStatus.Created, action) => Pass();

    internal AnonymousWorkloadImplWithDI(WorkloadStatus status, Action<IWorkloadServiceProvider> action) : base(status)
    {
        _action = action;
    }

    internal override void RegisterServiceProvider(IWorkloadServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    private protected override void ExecuteCore()
    {
        Debug.Assert(_serviceProvider is not null);
        _action(_serviceProvider!);
    }

    internal override void InternalRunContinuations(int workerId)
    {
        base.InternalRunContinuations(workerId);

        if (_pool is not null)
        {
            Volatile.Write(ref _action, null!);
            Volatile.Write(ref _status, WorkloadStatus.Pooled);
            // this is usually very risky, but we should be the only ones with a reference to this workload
            // so we can safely do this. otherwise, this would be illegal as it would violate the allowed
            // state transitions of the workload continuations.
            Volatile.Write(ref _continuation, null);
            _pool.Return(this);
        }
    }

    internal void Initialize(Action<IWorkloadServiceProvider> action)
    {
        Volatile.Write(ref _action, action);
        Volatile.Write(ref _status, WorkloadStatus.Created);
    }

    internal override nint GetPayloadFunctionPointer() => _action.Method.MethodHandle.GetFunctionPointer();
}
