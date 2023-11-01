using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.WorkloadTypes;

internal class AnonymousWorkloadImpl : AnonymousWorkload, IPoolableAnonymousWorkload<AnonymousWorkloadImpl>
{
    private readonly AnonymousWorkloadPool<AnonymousWorkloadImpl>? _pool;
    private Action _action;

    private AnonymousWorkloadImpl(AnonymousWorkloadPool<AnonymousWorkloadImpl> pool) : this(WorkloadStatus.Created, null!)
    {
        _pool = pool;
    }

    internal AnonymousWorkloadImpl(Action action) : this(WorkloadStatus.Created, action) => Pass();

    internal AnonymousWorkloadImpl(WorkloadStatus status, Action action) : base(status)
    {
        _action = action;
    }

    public static AnonymousWorkloadImpl Create(AnonymousWorkloadPool<AnonymousWorkloadImpl> pool) =>
        new(pool);

    private protected override void ExecuteCore() => _action();

    internal override void InternalRunContinuations()
    {
        if (_pool is not null)
        {
            Volatile.Write(ref _action, null!);
            Volatile.Write(ref _status, WorkloadStatus.Pooled);
            _pool.Return(this);
        }
    }

    internal void Initialize(Action action)
    {
        Volatile.Write(ref _action, action);
        Volatile.Write(ref _status, WorkloadStatus.Created);
    }
}
