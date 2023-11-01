using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClassfulQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassfulQdisc<THandle, TState, TQdisc>
{
    private protected readonly TQdisc _qdisc;

    private protected ClassfulQdiscBuilderBase(TQdisc qdisc)
    {
        _qdisc = qdisc;
    }
}

public sealed class ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent> : ClassfulQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassfulQdisc<THandle, TState, TQdisc>
    where TParent : class
{
    private readonly TParent _parent;

    internal ClassfulQdiscBuilder(TQdisc qdisc, TParent parent) : base(qdisc)
    {
        _parent = parent;
    }

    public TParent Build() => _parent;

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClasslessChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildState, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>> AddClassfulChild<TChildQdisc, TChildState>(THandle childHandle, Predicate<TChildState> childPredicate)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildState, TChildQdisc>
        where TChildState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildState, TChildQdisc, ClassfulQdiscBuilder<THandle, TState, TQdisc, TParent>>(qdisc, this);
    }
}

public sealed class ClassfulQdiscBuilderRoot<THandle, TState, TQdisc> : ClassfulQdiscBuilderBase<THandle, TState, TQdisc>
    where THandle : unmanaged
    where TState : class
    where TQdisc : class, IClassfulQdisc<THandle, TState, TQdisc>
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
        AnonymousWorkloadPoolManager? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPoolManager(_context.PoolSize);
        }
        return new ClassfulWorkloadFactory<THandle>(_qdisc, pool, _context.ContextOptions);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>> AddClasslessChild<TChildQdisc>(THandle childHandle)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>> AddClasslessChild<TChildQdisc>(THandle childHandle, Predicate<TState> childPredicate)
        where TChildQdisc : class, IClasslessQdisc<THandle, TChildQdisc>
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle);
        _qdisc.TryAddChild(qdisc, childPredicate);
        return new ClasslessQdiscBuilder<THandle, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }

    public ClassfulQdiscBuilder<THandle, TChildState, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>> AddClassfulChild<TChildQdisc, TChildState>(THandle childHandle, Predicate<TChildState> childPredicate)
        where TChildQdisc : class, IClassfulQdisc<THandle, TChildState, TChildQdisc>
        where TChildState : class
    {
        TChildQdisc qdisc = TChildQdisc.Create(childHandle, childPredicate);
        _qdisc.TryAddChild(qdisc);
        return new ClassfulQdiscBuilder<THandle, TChildState, TChildQdisc, ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>>(qdisc, this);
    }
}