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
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
public sealed class PrioFastLocking<THandle> : CustomClassfulQdiscBuilder<THandle, PrioFastLocking<THandle>>, ICustomClassfulQdiscBuilder<THandle, PrioFastLocking<THandle>>
    where THandle : unmanaged
{
    private IClasslessQdiscBuilder? _localQueueBuilder;
    private Predicate<object?>? _predicate;

    private readonly Dictionary<int, IClassifyingQdisc<THandle>> _children = [];

    private PrioFastLocking(THandle handle, IQdiscBuilderContext context) : base(handle, context) => Pass();

    public static PrioFastLocking<THandle> CreateBuilder(THandle handle, IQdiscBuilderContext context) =>
       new(handle, context);

    public PrioFastLocking<THandle> WithClassificationPredicate(Predicate<object?> predicate)
    {
        _predicate = predicate;
        return this;
    }

    public PrioFastLocking<THandle> WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public PrioFastLocking<THandle> WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private PrioFastLocking<THandle> WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
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

    public PrioFastLocking<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, priority, null, null);

    public PrioFastLocking<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, priority, null, configureChild);

    public PrioFastLocking<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, priority, configureClassification, null);

    public PrioFastLocking<THandle> AddClasslessChild<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, priority, configureClassification, configureChild);

    private PrioFastLocking<THandle> AddClasslessChildCore<TChild>(THandle childHandle, int priority, Action<SimplePredicateBuilder>? configureClassification, Action<TChild>? configureChild)
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

    public PrioFastLocking<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority)
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

    public PrioFastLocking<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority, Action<TChild> configureChild)
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

    public PrioFastLocking<THandle> AddClassfulChild<TChild>(THandle childHandle, int priority, Action<ClassfulBuilder<THandle, SimplePredicateBuilder, TChild>> configureChild)
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
        return new PrioFastLockingQdisc<THandle>(handle, _predicate, _localQueueBuilder, children, _context.MaximumConcurrency);
    }
}