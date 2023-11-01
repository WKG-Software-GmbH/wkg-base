using System.Diagnostics;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadImplWithDI : AnonymousWorkload, IPoolableAnonymousWorkload<AnonymousWorkloadImplWithDI>
{
    private readonly AnonymousWorkloadPool<AnonymousWorkloadImplWithDI>? _pool;
    private Action<IWorkloadServiceProvider> _action;
    private IWorkloadServiceProvider? _serviceProvider;

    private AnonymousWorkloadImplWithDI(AnonymousWorkloadPool<AnonymousWorkloadImplWithDI> pool) : this(WorkloadStatus.Created, null!)
    {
        _pool = pool;
    }

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

    public static AnonymousWorkloadImplWithDI Create(AnonymousWorkloadPool<AnonymousWorkloadImplWithDI> pool) =>
        new(pool);

    internal override void InternalRunContinuations()
    {
        if (_pool is not null)
        {
            Volatile.Write(ref _action, null!);
            Volatile.Write(ref _status, WorkloadStatus.Pooled);
            _pool.Return(this);
        }
    }

    internal void Initialize(Action<IWorkloadServiceProvider> action)
    {
        Volatile.Write(ref _action, action);
        Volatile.Write(ref _status, WorkloadStatus.Created);
    }
}
