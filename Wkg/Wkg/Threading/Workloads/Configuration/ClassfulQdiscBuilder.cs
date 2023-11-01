using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classifiers;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
{
    private protected readonly TQdisc _qdisc;

    private protected ClassfulQdiscBuilderBase(TQdisc qdisc)
    {
        _qdisc = qdisc;
    }
}

public sealed class ClassfulQdiscBuilder<THandle, TQdisc, TParent> : ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
    where TParent : class
{
    private readonly TParent _parent;

    internal ClassfulQdiscBuilder(TQdisc qdisc, TParent parent) : base(qdisc)
    {
        _parent = parent;
    }

    public TParent Build() => _parent;

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>> AddClassfulChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>>(qdisc, this);
    }

    public ClassifyingQdiscBuilder<THandle, TState, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>> AddClassifyingChild<TChildQdisc, TState>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClassifyingQdisc<THandle, TState, TChildQdisc>
        where TState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassifyingQdiscBuilder<THandle, TState, TChildQdisc, ClassfulQdiscBuilder<THandle, TQdisc, TParent>>(qdisc, this);
    }
}

public sealed class ClassfulQdiscBuilderRoot<THandle, TQdisc> : ClassfulQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClassfulQdisc<THandle, TQdisc>
{
    private readonly QdiscBuilderContext _context;

    internal ClassfulQdiscBuilderRoot(TQdisc qdisc, QdiscBuilderContext context) : base(qdisc)
    {
        _context = context;
    }

    public ClassfulWorkloadFactory<THandle> Build()
    {
        WorkloadScheduler scheduler = new(_qdisc, _context.MaximumConcurrency);
        _qdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPool? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPool(_context.PoolSize);
        }
        return new ClassfulWorkloadFactory<THandle>(_qdisc, pool, _context.ContextOptions);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>> AddClassfulChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>>(qdisc, this);
    }

    public ClassifyingQdiscBuilder<THandle, TState, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>> AddClassifyingChild<TChildQdisc, TState>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClassifyingQdisc<THandle, TState, TChildQdisc>
        where TState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassifyingQdiscBuilder<THandle, TState, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TQdisc>>(qdisc, this);
    }
}