using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless.Qdiscs;

namespace Wkg.Threading.Workloads.Queuing.Classless;

public interface IClasslessQdisc : IQdisc
{
    /// <summary>
    /// Enqueues the workload to be executed onto this qdisc.
    /// </summary>
    /// <param name="workload">The workload to be enqueued.</param>
    internal void Enqueue(AbstractWorkloadBase workload);
}

public interface IClasslessQdisc<THandle> : IClasslessQdisc, IQdisc<THandle> 
    where THandle : unmanaged
{
}

public interface IClasslessQdisc<THandle, TQdisc> : IClasslessQdisc<THandle>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    /// <summary>
    /// Creates a new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">The handle uniquely identifying this qdisc. The handle must not be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c> and must not be used by any other qdisc.</param>
    /// <returns>A new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/>.</returns>
    static abstract TQdisc Create(THandle handle);

    /// <summary>
    /// Creates a new anonymous <typeparamref name="TQdisc"/> instance. The handle is not used for classification and may be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c>.
    /// </summary>
    /// <returns>A new anonymous <typeparamref name="TQdisc"/> instance.</returns>
    static abstract TQdisc CreateAnonymous();
}

abstract class ClasslessQdiscBuilder<TSelf> where TSelf : ClasslessQdiscBuilder<TSelf>
{
    private readonly IPredicateBuilder _predicateBuilder = new PredicateBuilder();

    internal Predicate<object?>? Predicate { get; private set; }

    protected ClasslessQdiscBuilder()
    {
    }

    // TODO: add an extension point to allow for dynamicly compiled predicates (e.g., expression trees / IL emit)
    public TSelf WithClassificationPredicate<TState>(Predicate<TState> predicate)
    {
        _predicateBuilder.AddPredicate(predicate);
        return ReinterpretCast<TSelf>(this);
    }

    protected abstract IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle) where THandle : unmanaged;

    internal IClasslessQdisc<THandle> Build<THandle>(THandle handle) where THandle : unmanaged
    {
        Predicate = _predicateBuilder.Compile();
        return BuildInternal(handle);
    }
}

interface IClasslessQdiscBuilder<TSelf> where TSelf : ClasslessQdiscBuilder<TSelf>, IClasslessQdiscBuilder<TSelf>
{
    static abstract TSelf CreateBuilder();
}

class QFQ : ClasslessQdiscBuilder<QFQ>, IClasslessQdiscBuilder<QFQ>
{
    internal QFQ() : base()
    {
    }

    public QFQ ConfigureFoo(int foo)
    {
        return this;
    }

    public QFQ ConfigureBar(string bar)
    {
        return this;
    }

    protected override IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle)
    {
        return FifoQdisc<THandle>.Create(handle);
    }

    public static QFQ CreateBuilder() => new();
}

class TestStuff<THandle> where THandle : unmanaged
{
    public TestStuff<THandle> AddClasslessChild<TChild>(THandle childHandle, Action<TChild> childConfigurationAction)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild>
    {
        TChild childBuilder = TChild.CreateBuilder();
        childConfigurationAction(childBuilder);
        IClasslessQdisc<THandle> qdisc = childBuilder.Build(childHandle);
        return this;
    }
}

class ASdf
{
    void A()
    {
        TestStuff<int> builder = new();
        builder.AddClasslessChild<QFQ>(1, qfq => qfq
            .ConfigureFoo(1)
            .ConfigureBar("asdf"));
    }
}