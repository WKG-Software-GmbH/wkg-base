using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration;

public interface IClassfulQdiscBuilder<TSelf> where TSelf : ClassfulQdiscBuilder<TSelf>, IClassfulQdiscBuilder<TSelf>
{
    static abstract TSelf CreateBuilder(IQdiscBuilderContext context);
}

public abstract class ClassfulQdiscBuilder<TSelf> where TSelf : ClassfulQdiscBuilder<TSelf>, IClassfulQdiscBuilder<TSelf>
{
    internal protected abstract IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate) where THandle : unmanaged;

    internal IClassfulQdisc<THandle> Build<THandle>(THandle handle, Predicate<object?> predicate) where THandle : unmanaged
    {
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);

        return BuildInternal(handle, predicate);
    }
}

public sealed class ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc>
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
    where TQdisc : ClassfulQdiscBuilder<TQdisc>, IClassfulQdiscBuilder<TQdisc>
{
    private readonly QdiscBuilderContext _context;
    private readonly THandle _handle;
    private readonly List<(IClasslessQdisc<THandle>, Predicate<object?>?)> _children = new();
    private readonly TPredicateBuilder _predicateBuilder = new();
    private TQdisc? _qdiscBuilder;

    internal ClassfulQdiscBuilder(THandle handle, QdiscBuilderContext context)
    {
        _handle = handle;
        _context = context;
    }

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, null, null);

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, null, configureChild);

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TPredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, configureClassification, null);

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChild<TChild>(THandle childHandle, Action<TPredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, configureClassification, configureChild);

    private ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClasslessChildCore<TChild>(THandle childHandle, Action<TPredicateBuilder>? configureClassification, Action<TChild>? configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild>
    {
        TChild childBuilder = TChild.CreateBuilder(_context);
        if (configureChild is not null)
        {
            configureChild(childBuilder);
        }
        IClasslessQdisc<THandle> qdisc = childBuilder.Build(childHandle);
        Predicate<object?>? predicate = null;
        if (configureClassification is not null)
        {
            TPredicateBuilder predicateBuilder = new();
            configureClassification(predicateBuilder);
            predicate = predicateBuilder.Compile();
        }
        _children.Add((qdisc, predicate));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClassfulChild<TChild>(THandle childHandle)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ClassfulQdiscBuilder<THandle, TPredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add((qdisc, null));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> AddClassfulChild<TChild>(THandle childHandle, Action<ClassfulQdiscBuilder<THandle, TPredicateBuilder, TChild>> configureChild)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ClassfulQdiscBuilder<THandle, TPredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add((qdisc, null));
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> ConfigureClassificationPredicates(Action<TPredicateBuilder> classifier)
    {
        classifier(_predicateBuilder);
        return this;
    }

    public ClassfulQdiscBuilder<THandle, TPredicateBuilder, TQdisc> ConfigureQdisc(Action<TQdisc> configureQdisc)
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

        Predicate<object?> predicate = _predicateBuilder.Compile() ?? NoMatch;
        IClassfulQdisc<THandle> qdisc = _qdiscBuilder.Build(_handle, predicate);
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

    private static bool NoMatch(object? _) => false;
}