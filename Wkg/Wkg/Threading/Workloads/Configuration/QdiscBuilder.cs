using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Configuration;

public class QdiscBuilder<THandle> where THandle : unmanaged
{
    // TODO: configure workload pooling
    private readonly QdiscBuilderContext _context = new();

    public QdiscBuilder<THandle> WithMaximumConcurrency(int maximumConcurrency)
    {
        _context.MaximumConcurrency = maximumConcurrency;
        return this;
    }

    public ClasslessQdiscBuilderRoot<THandle, TQdisc> WithClasslessRoot<TQdisc>(THandle rootHandle) 
        where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
    {
        TQdisc qdisc = TQdisc.Create(rootHandle);
        return new ClasslessQdiscBuilderRoot<THandle, TQdisc>(qdisc, _context);
    }

    public ClassfulQdiscBuilderRoot<THandle, TQdisc> WithClassfulRoot<TQdisc>(THandle rootHandle) 
        where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
    {
        TQdisc qdisc = TQdisc.Create(rootHandle);
        return new ClassfulQdiscBuilderRoot<THandle, TQdisc>(qdisc, _context);
    }

    public ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc> WithClassifyingRoot<TQdisc, TState>(THandle rootHandle, Predicate<TState> rootPredicate) 
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
}