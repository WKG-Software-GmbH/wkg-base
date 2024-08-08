using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classful.Custom;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.PrioFast;

/// <summary>
/// A classful qdisc that implements a simple priority scheduling algorithm to dequeue workloads from its children.
/// </summary>
public sealed class PrioFast<THandle> : CustomClassfulQdiscBuilder<THandle, PrioFast<THandle>>, ICustomClassfulQdiscBuilder<THandle, PrioFast<THandle>>
    where THandle : unmanaged
{
    private IClasslessQdiscBuilder? _localQueueBuilder;
    private Predicate<object?>? _predicate;
    private bool _expectHighContention;
    private readonly Dictionary<int, IClassifyingQdisc<THandle>> _children = [];

    private PrioFast(THandle handle, IQdiscBuilderContext context) : base(handle, context) => Pass();

    public static PrioFast<THandle> CreateBuilder(THandle handle, IQdiscBuilderContext context) =>
       new(handle, context);

    public PrioFast<THandle> WithClassificationPredicate(Predicate<object?> predicate)
    {
        _predicate = predicate;
        return this;
    }

    public PrioFast<THandle> WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public PrioFast<THandle> WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private PrioFast<THandle> WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue>
    {
        if (_localQueueBuilder is not null)
        {
            throw new InvalidOperationException("Local queue has already been configured.");
        }

        TLocalQueue localQueueBuilder = TLocalQueue.CreateBuilder(_context);
        configureLocalQueue?.Invoke(localQueueBuilder);
        _localQueueBuilder = localQueueBuilder;

        return this;
    }

    /// <summary>
    /// Optimizes the qdisc for high contention scenarios with a large number of workers and workloads.
    /// </summary>
    /// <param name="expectHighContention">Whether to optimize for high contention scenarios.</param>
    /// <returns>The current instance of the builder.</returns>
    public PrioFast<THandle> OptimizeForHighContention(bool expectHighContention = true)
    {
        _expectHighContention = expectHighContention;
        return this;
    }

    public PrioFast<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, priority, null, null);

    public PrioFast<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, priority, null, configureChild);

    public PrioFast<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, priority, configureClassification, null);

    public PrioFast<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, priority, configureClassification, configureChild);

    private PrioFast<THandle> AddClasslessChildCore<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder>? configureClassification, Action<TChild>? configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild>
    {
        if (_children.ContainsKey(priority))
        {
            throw new InvalidOperationException($"A child with priority {priority} has already been added.");
        }

        TChild childBuilder = TChild.CreateBuilder(_context);
        if (configureChild is not null)
        {
            configureChild(childBuilder);
        }
        Predicate<object?>? predicate = null;
        if (configureClassification is not null)
        {
            SimplePredicateBuilder predicateBuilder = new();
            configureClassification(predicateBuilder);
            predicate = predicateBuilder.Compile();
        }
        IClassifyingQdisc<THandle> qdisc = childBuilder.Build(childHandle, predicate);
        _children.Add(priority, qdisc);
        return this;
    }

    public PrioFast<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        if (_children.ContainsKey(priority))
        {
            throw new InvalidOperationException($"A child with priority {priority} has already been added.");
        }

        ClassfulBuilder<THandle, SimplePredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add(priority, qdisc);
        return this;
    }

    public PrioFast<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority, Action<TChild> configureChild)
        where TChild : CustomClassfulQdiscBuilder<THandle, TChild>, ICustomClassfulQdiscBuilder<THandle, TChild>
    {
        if (_children.ContainsKey(priority))
        {
            throw new InvalidOperationException($"A child with priority {priority} has already been added.");
        }

        TChild childBuilder = TChild.CreateBuilder(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add(priority, qdisc);
        return this;
    }

    public PrioFast<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority, Action<ClassfulBuilder<THandle, SimplePredicateBuilder, TChild>> configureChild)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        if (_children.ContainsKey(priority))
        {
            throw new InvalidOperationException($"A child with priority {priority} has already been added.");
        }

        ClassfulBuilder<THandle, SimplePredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add(priority, qdisc);
        return this;
    }

    protected override IClassfulQdisc<THandle> BuildInternal(THandle handle)
    {
        _localQueueBuilder ??= Fifo.CreateBuilder(_context);
        IClassifyingQdisc<THandle>[] children = _children.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
        return _expectHighContention
            ? new PrioFastBitmapQdisc<THandle>(handle, _predicate, _localQueueBuilder, children, _context.MaximumConcurrency)
            : new PrioFastLockingBitmapQdisc<THandle>(handle, _predicate, _localQueueBuilder, children, _context.MaximumConcurrency);
    }
}