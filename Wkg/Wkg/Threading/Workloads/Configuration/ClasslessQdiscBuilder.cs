using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    private protected readonly THandle _handle;

    private protected ClasslessQdiscBuilderBase(THandle handle)
    {
        _handle = handle;
    }
}

public sealed class ClasslessQdiscBuilder<THandle, TQdisc> : ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    private readonly IPredicateBuilder _predicateBuilder = new PredicateBuilder();

    internal Predicate<object?>? Predicate { get; private set; }

    internal ClasslessQdiscBuilder(THandle handle) : base(handle) => Pass();

    public ClasslessQdiscBuilder<THandle, TQdisc> WithClassificationPredicate<TState>(Predicate<TState> predicate) where TState : class
    {
        _predicateBuilder.AddPredicate(predicate);
        return this;
    }

    internal TQdisc Build()
    {
        Predicate = _predicateBuilder.Compile();
        return TQdisc.Create(_handle);
    }
}

public sealed class ClasslessQdiscBuilderRoot<THandle, TQdisc> : ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    private readonly QdiscBuilderContext _context;

    internal ClasslessQdiscBuilderRoot(THandle handle, QdiscBuilderContext context) : base(handle)
    {
        _context = context;
    }

    public ClasslessWorkloadFactory<THandle> Build()
    {
        TQdisc qdisc = TQdisc.Create(_handle);
        WorkloadScheduler scheduler = _context.ServiceProviderFactory is null
            ? new WorkloadScheduler(qdisc, _context.MaximumConcurrency)
            : new WorkloadSchedulerWithDI(qdisc, _context.MaximumConcurrency, _context.ServiceProviderFactory);
        qdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPoolManager? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPoolManager(_context.PoolSize);
        }
        return new ClasslessWorkloadFactory<THandle>(qdisc, pool, _context.ContextOptions);
    }
}