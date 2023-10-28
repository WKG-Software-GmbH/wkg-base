using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Configuration;

public class QdiscBuilder<THandle> where THandle : unmanaged
{
    private readonly QdiscBuilderContext _context = new();

    public QdiscBuilder<THandle> UseMaximumConcurrency(int maximumConcurrency)
    {
        _context.MaximumConcurrency = maximumConcurrency;
        return this;
    }

    public QdiscBuilder<THandle> UseAnonymousWorkloadPooling(int poolSize = 64)
    {
        _context.PoolSize = poolSize;
        return this;
    }

    public ClasslessQdiscBuilderRoot<THandle, TQdisc> UseClasslessRoot<TQdisc>(THandle rootHandle) 
        where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
    {
        TQdisc qdisc = TQdisc.Create(rootHandle);
        return new ClasslessQdiscBuilderRoot<THandle, TQdisc>(qdisc, _context);
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc> UseClassfulRoot<TQdisc>(THandle rootHandle) 
        where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
    {
        TQdisc qdisc = TQdisc.Create(rootHandle);
        return new ClassfulQdiscBuilderRoot<THandle, TQdisc>(qdisc, _context);
    }

    public ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc> UseClassifyingRoot<TQdisc, TState>(THandle rootHandle, Predicate<TState> rootPredicate) 
        where TQdisc : class, IClassifyingQdisc<THandle, TState, TQdisc>
        where TState : class
    {
        TQdisc qdisc = TQdisc.Create(rootHandle, rootPredicate);
        return new ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>(qdisc, _context);
    }
}

internal class QdiscBuilderContext
{
    public int MaximumConcurrency { get; set; } = 2;

    public int PoolSize { get; set; } = -1;

    public bool UsePooling => PoolSize > 0;
}