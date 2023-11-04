using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
{
    private protected readonly THandle _handle;

    private protected ClassfulQdiscBuilderBase(THandle handle)
    {
        _handle = handle;
    }
}

public sealed class ClassfulQdiscBuilder<THandle, TQdisc> : ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
{
    internal Predicate<object?>? Predicate { get; private set; }

    private readonly IPredicateBuilder _predicateBuilder = new PredicateBuilder();
    private readonly List<(IClasslessQdisc<THandle>, Predicate<object?>?)> _children = new();

    internal ClassfulQdiscBuilder(THandle handle) : base(handle)
    {
    }

    public ClassfulQdiscBuilder<THandle, TQdisc> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        ClasslessQdiscBuilder<THandle, TChildQdisc> childBuilder = new(childHandle);
        TChildQdisc qdisc = childBuilder.Build();
        _children.Add((qdisc, null));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TQdisc> AddClasslessChild<TChildQdisc>(THandle childHandle, Action<ClasslessQdiscBuilder<THandle, TChildQdisc>> configureChild)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        ClasslessQdiscBuilder<THandle, TChildQdisc> childBuilder = new(childHandle);
        configureChild(childBuilder);
        TChildQdisc qdisc = childBuilder.Build();
        _children.Add((qdisc, childBuilder.Predicate));
        AddClasslessChild(childHandle, new TestQdisc<THandle>());
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TQdisc> AddClasslessChild<TChildQdisc, TBuilder>(THandle childHandle, TChildQdisc child)
        where TChildQdisc : IClasslessQdiscProvider<THandle, TBuilder>
        where TBuilder : class
    {
        TBuilder builder = TChildQdisc.CreateBuilderFactory(childHandle);
        configureChild(childBuilder);
        TChildQdisc qdisc = childBuilder.Build();
        _children.Add((qdisc, childBuilder.Predicate));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TQdisc> AddClassfulChild<TChildQdisc>(THandle childHandle, Action<ClassfulQdiscBuilder<THandle, TChildQdisc>> configureChild)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        ClassfulQdiscBuilder<THandle, TChildQdisc> builder = new(childHandle);
        configureChild(builder);
        TChildQdisc qdisc = builder.Build();
        _children.Add((qdisc, builder.Predicate));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TQdisc> AddClassificationPredicate<TState>(Predicate<TState> predicate) where TState : class
    {
        _predicateBuilder.AddPredicate(predicate);
        return this;
    }

    internal TQdisc Build()
    {
        Predicate = _predicateBuilder.Compile();
        // TODO: this sucks
        Predicate<object?> predicate = _predicateBuilder.Compile() ?? new Predicate<object?>(_ => false);
        TQdisc qdisc = TQdisc.Create(_handle, predicate);
        foreach ((IClasslessQdisc<THandle> child, Predicate<object?>? childPredicate) in _children)
        {
            if (child is IClassfulQdisc<THandle> classfulChild)
            {
                qdisc.TryAddChild(classfulChild);
            }
            else if (childPredicate is not null)
            {
                qdisc.TryAddChild(child, childPredicate);
            }
            else
            {
                qdisc.TryAddChild(child);
            }
        }
        return qdisc;
    }
}

public sealed class ClassfulQdiscBuilderRoot<THandle, TQdisc, TFactory> : ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
    where TFactory : AbstractClassfulWorkloadFactory<THandle>, IWorkloadFactory<THandle, TFactory>
{
    private readonly QdiscBuilderContext _context;
    private readonly IPredicateBuilder _predicateBuilder = new PredicateBuilder();
    private readonly List<(IClasslessQdisc<THandle>, Predicate<object?>?)> _children = new();

    internal ClassfulQdiscBuilderRoot(THandle handle, QdiscBuilderContext context) : base(handle)
    {
        _context = context;
    }

    public TFactory Build()
    {
        // TODO: this sucks
        Predicate<object?> predicate = _predicateBuilder.Compile() ?? new Predicate<object?>(_ => false);
        TQdisc qdisc = TQdisc.Create(_handle, predicate);
        foreach ((IClasslessQdisc<THandle> child, Predicate<object?>? childPredicate) in _children)
        {
            if (child is IClassfulQdisc<THandle> classfulChild)
            {
                qdisc.TryAddChild(classfulChild);
            }
            else if (childPredicate is not null)
            {
                qdisc.TryAddChild(child, childPredicate);
            }
            else
            {
                qdisc.TryAddChild(child);
            }
        }
        WorkloadScheduler scheduler = _context.ServiceProviderFactory is null
            ? new WorkloadScheduler(qdisc, _context.MaximumConcurrency)
            : new WorkloadSchedulerWithDI(qdisc, _context.MaximumConcurrency, _context.ServiceProviderFactory);
        qdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPoolManager? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPoolManager(_context.PoolSize);
        }
        return TFactory.Create(qdisc, pool, _context.ContextOptions);
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, TFactory> AddClassificationPredicate<TState>(Predicate<TState> predicate) where TState : class
    {
        _predicateBuilder.AddPredicate(predicate);
        return this;
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, TFactory> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        ClasslessQdiscBuilder<THandle, TChildQdisc> childBuilder = new(childHandle);
        TChildQdisc qdisc = childBuilder.Build();
        _children.Add((qdisc, null));
        return this;
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, TFactory> AddClasslessChild<TChildQdisc>(THandle childHandle, Action<ClasslessQdiscBuilder<THandle, TChildQdisc>> configureChild)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        ClasslessQdiscBuilder<THandle, TChildQdisc> childBuilder = new(childHandle);
        configureChild(childBuilder);
        TChildQdisc qdisc = childBuilder.Build();
        _children.Add((qdisc, childBuilder.Predicate));
        return this;
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, TFactory> AddClassfulChild<TChildQdisc>(THandle childHandle, Action<ClassfulQdiscBuilder<THandle, TChildQdisc>> configureChild)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        ClassfulQdiscBuilder<THandle, TChildQdisc> builder = new(childHandle);
        configureChild(builder);
        TChildQdisc qdisc = builder.Build();
        _children.Add((qdisc, builder.Predicate));
        return this;
    }
}