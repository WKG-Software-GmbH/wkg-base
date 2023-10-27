using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Internals;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    private protected readonly TQdisc _qdisc;

    private protected ClasslessQdiscBuilderBase(TQdisc qdisc)
    {
        _qdisc = qdisc;
    }
}

public sealed class ClasslessQdiscBuilder<THandle, TQdisc, TParent> : ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
    where TParent : class
{
    private readonly TParent _parent;

    internal ClasslessQdiscBuilder(TQdisc qdisc, TParent parent) : base(qdisc)
    {
        _parent = parent;
    }

    public TParent Build() => _parent;
}

public sealed class ClasslessQdiscBuilderRoot<THandle, TQdisc> : ClasslessQdiscBuilderBase<THandle, TQdisc>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    private readonly QdiscBuilderContext _context;

    internal ClasslessQdiscBuilderRoot(TQdisc qdisc, QdiscBuilderContext context) : base(qdisc)
    {
        _context = context;
    }

    public ClasslessWorkloadFactory<THandle> Build()
    {
        WorkloadScheduler scheduler = new(_qdisc, _context.MaximumConcurrency);
        _qdisc.InternalInitialize(scheduler);
        return new ClasslessWorkloadFactory<THandle>(_qdisc);
    }
}