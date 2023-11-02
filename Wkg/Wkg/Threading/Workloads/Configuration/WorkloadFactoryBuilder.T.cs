using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.DependencyInjection.Configuration;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.DependencyInjection.Implementations;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class WorkloadFactoryBuilderBase<THandle, TSelf> 
    where THandle : unmanaged
    where TSelf : WorkloadFactoryBuilderBase<THandle, TSelf>
{
    private protected readonly QdiscBuilderContext _context;

    private protected WorkloadFactoryBuilderBase(QdiscBuilderContext? context = null)
    {
        if (!GetType().Equals(typeof(TSelf)))
        {
            throw new InvalidOperationException($"The type parameter {typeof(TSelf).Name} does not match the type of the current builder {GetType().Name}.");
        }
        _context ??= context ?? new QdiscBuilderContext();
    }

    public TSelf UseMaximumConcurrency(int maximumConcurrency)
    {
        _context.MaximumConcurrency = maximumConcurrency;
        return this.To<TSelf>();
    }

    public TSelf UseWorkloadContextOptions(WorkloadContextOptions options)
    {
        _context.ContextOptions = options;
        return this.To<TSelf>();
    }

    public TSelf FlowExecutionContextToContinuations(bool flowExecutionContext = true)
    {
        _context.ContextOptions = _context.ContextOptions with { FlowExecutionContext = flowExecutionContext };
        return this.To<TSelf>();
    }

    public TSelf RunContinuationsOnCapturedContext(bool continueOnCapturedContext = true)
    {
        _context.ContextOptions = _context.ContextOptions with { ContinueOnCapturedContext = continueOnCapturedContext };
        return this.To<TSelf>();
    }

    public TSelf UseAnonymousWorkloadPooling(int poolSize = 64)
    {
        _context.PoolSize = poolSize;
        return this.To<TSelf>();
    }
}

public class WorkloadFactoryBuilder<THandle> : WorkloadFactoryBuilderBase<THandle, WorkloadFactoryBuilder<THandle>> where THandle : unmanaged
{
    internal WorkloadFactoryBuilder(QdiscBuilderContext context) : base(context) => Pass();

    internal WorkloadFactoryBuilder() => Pass();

    public WorkloadFactoryBuilderWithDI<THandle> UseDependencyInjection(Action<WorkloadServiceProviderBuilder> configurationAction) =>
        UseDependencyInjection<SimpleWorkloadServiceProviderFactory>(configurationAction);

    public WorkloadFactoryBuilderWithDI<THandle> UseDependencyInjection<TServiceProviderFactory>(Action<WorkloadServiceProviderBuilder> configurationAction)
        where TServiceProviderFactory : class, IWorkloadServiceProviderFactory, new()
    {
        IWorkloadServiceProviderFactory factoryProvider = new TServiceProviderFactory();
        WorkloadServiceProviderBuilder builder = new(factoryProvider);
        configurationAction.Invoke(builder);
        _context.ServiceProviderFactory = builder.Build();
        return new WorkloadFactoryBuilderWithDI<THandle>(_context);
    }

    public ClasslessQdiscBuilderRoot<THandle, TQdisc, ClasslessWorkloadFactory<THandle>> UseClasslessRoot<TQdisc>(THandle rootHandle)
        where TQdisc : class, IClasslessQdisc<THandle, TQdisc> => 
        new(rootHandle, _context);

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, ClassfulWorkloadFactory<THandle>> UseClassfulRoot<TQdisc>(THandle rootHandle)
        where TQdisc : class, IClassfulQdisc<THandle, TQdisc> =>
        new(rootHandle, _context);
}

public class WorkloadFactoryBuilderWithDI<THandle> : WorkloadFactoryBuilderBase<THandle, WorkloadFactoryBuilderWithDI<THandle>> where THandle : unmanaged
{
    internal WorkloadFactoryBuilderWithDI(QdiscBuilderContext context) : base(context) => Pass();

    public ClasslessQdiscBuilderRoot<THandle, TQdisc, ClasslessWorkloadFactoryWithDI<THandle>> UseClasslessRoot<TQdisc>(THandle rootHandle)
        where TQdisc : class, IClasslessQdisc<THandle, TQdisc> =>
        new(rootHandle, _context);

    public ClassfulQdiscBuilderRoot<THandle, TQdisc, ClassfulWorkloadFactoryWithDI<THandle>> UseClassfulRoot<TQdisc>(THandle rootHandle)
        where TQdisc : class, IClassfulQdisc<THandle, TQdisc> =>
        new(rootHandle, _context);
}