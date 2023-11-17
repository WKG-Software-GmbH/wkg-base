using Wkg.Threading.Workloads.Configuration.Classful.Custom;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classful;

public sealed class ClassfulBuilder<THandle, TPredicateBuilder, TQdisc>
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
    where TQdisc : ClassfulQdiscBuilder<TQdisc>, IClassfulQdiscBuilder<TQdisc>
{
    private readonly QdiscBuilderContext _context;
    private readonly THandle _handle;
    private readonly List<IClassifyingQdisc<THandle>> _children = [];
    private readonly TPredicateBuilder _predicateBuilder = new();
    private TQdisc? _qdiscBuilder;

    internal ClassfulBuilder(THandle handle, QdiscBuilderContext context)
    {
        _handle = handle;
        _context = context;
    }

    public ClassfulBuilder(THandle handle, IQdiscBuilderContext context) : this(handle, (QdiscBuilderContext)context) => Pass();

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, null, null);

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, null, configureChild);

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TPredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, configureClassification, null);

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TPredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, configureClassification, configureChild);

    private ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChildCore<TChild>(THandle childHandle, Action<TPredicateBuilder>? configureClassification, Action<TChild>? configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild>
    {
        TChild childBuilder = TChild.CreateBuilder(_context);
        if (configureChild is not null)
        {
            configureChild(childBuilder);
        }
        Predicate<object?>? predicate = null;
        if (configureClassification is not null)
        {
            TPredicateBuilder predicateBuilder = new();
            configureClassification(predicateBuilder);
            predicate = predicateBuilder.Compile();
        }
        IClassifyingQdisc<THandle> child = childBuilder.Build(childHandle, predicate);
        _children.Add(child);
        return this;
    }

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClassfulChild<TChild>(THandle childHandle)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ClassfulBuilder<THandle, TPredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        IClassfulQdisc<THandle> child = childBuilder.Build();
        _children.Add(child);
        return this;
    }

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClassfulChild<TChild>(THandle childHandle, Action<TChild> configureChild)
        where TChild : CustomClassfulQdiscBuilder<THandle, TChild>, ICustomClassfulQdiscBuilder<THandle, TChild>
    {
        TChild childBuilder = TChild.CreateBuilder(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> child = childBuilder.Build();
        _children.Add(child);
        return this;
    }

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> AddClassfulChild<TChild>(THandle childHandle, Action<ClassfulBuilder<THandle, TPredicateBuilder, TChild>> configureChild)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ClassfulBuilder<THandle, TPredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> child = childBuilder.Build();
        _children.Add(child);
        return this;
    }

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> ConfigureClassificationPredicates(Action<TPredicateBuilder> classifier)
    {
        classifier(_predicateBuilder);
        return this;
    }

    public ClassfulBuilder<THandle, TPredicateBuilder, TQdisc> ConfigureQdisc(Action<TQdisc> configureQdisc)
    {
        if (_qdiscBuilder is not null)
        {
            throw new WorkloadSchedulingException("Qdisc has already been configured.");
        }

        _qdiscBuilder = TQdisc.CreateBuilder(_context);
        configureQdisc(_qdiscBuilder);
        return this;
    }

    internal IClassfulQdisc<THandle> Build()
    {
        _qdiscBuilder ??= TQdisc.CreateBuilder(_context);

        Predicate<object?>? predicate = _predicateBuilder.Compile();
        IClassfulQdisc<THandle> qdisc = _qdiscBuilder.Build(_handle, predicate);
        foreach (IClassifyingQdisc<THandle> child in _children)
        {
            qdisc.TryAddChild(child);
        }
        return qdisc;
    }
}