using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClassifyingQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassifyingQdisc<THandle, TState, TQdisc>
{
    private protected readonly TQdisc _qdisc;

    private protected ClassifyingQdiscBuilderBase(TQdisc qdisc)
    {
        _qdisc = qdisc;
    }
}

public sealed class ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent> : ClassifyingQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassifyingQdisc<THandle, TState, TQdisc>
    where TParent : class
{
    private readonly TParent _parent;

    internal ClassifyingQdiscBuilder(TQdisc qdisc, TParent parent) : base(qdisc)
    {
        _parent = parent;
    }

    public TParent Build() => _parent;

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClasslessChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClassfulChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClassfulChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClassifyingQdiscBuilder<THandle, TChildState, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClassifyingChild<TChildQdisc, TChildState>(THandle childHandle, Predicate<TChildState> childPredicate)
        where TChildQdisc : class, IClassifyingQdisc<THandle, TChildState, TChildQdisc>
        where TChildState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassifyingQdiscBuilder<THandle, TChildState, TChildQdisc, ClassifyingQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }
}

public sealed class ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc> : ClassifyingQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassifyingQdisc<THandle, TState, TQdisc>
{
    private readonly QdiscBuilderContext _context;

    internal ClassifyingQdiscBuilderRoot(TQdisc qdisc, QdiscBuilderContext context) : base(qdisc)
    {
        _context = context;
    }

    public ClassifyingWorkloadFactory<THandle> Build()
    {
        WorkloadScheduler scheduler = new(_qdisc, _context.MaximumConcurrency);
        _qdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPool? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPool(_context.PoolSize);
        }
        return new ClassifyingWorkloadFactory<THandle>(_qdisc, pool, _context.ContextOptions);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>> AddClasslessChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>> AddClassfulChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>> AddClassfulChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClassifyingQdiscBuilder<THandle, TChildState, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>> AddClassifyingChild<TChildQdisc, TChildState>(THandle childHandle, Predicate<TChildState> childPredicate)
        where TChildQdisc : class, IClassifyingQdisc<THandle, TChildState, TChildQdisc>
        where TChildState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassifyingQdiscBuilder<THandle, TChildState, TChildQdisc, ClassifyingQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }
}