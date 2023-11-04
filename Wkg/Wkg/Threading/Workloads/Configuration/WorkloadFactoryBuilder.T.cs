using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.DependencyInjection.Configuration;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Common.Extensions;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.DependencyInjection.Implementations;
using Wkg.Threading.Workloads.Scheduling;
using Wkg.Threading.Workloads.WorkloadTypes.Pooling;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;

namespace Wkg.Threading.Workloads.Configuration;

public abstract class WorkloadFactoryBuilderBase<THandle, TPredicateBuilder, TSelf> 
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
    where TSelf : WorkloadFactoryBuilderBase<THandle, TPredicateBuilder, TSelf>
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

    private protected TWorkloadFactory UseClasslessRootCore<TWorkloadFactory, TRoot>(THandle rootHandle, Action<TRoot> rootConfiguration)
        where TRoot : ClasslessQdiscBuilder<TRoot>, IClasslessQdiscBuilder<TRoot>
        where TWorkloadFactory : AbstractClasslessWorkloadFactory<THandle>, IWorkloadFactory<THandle, TWorkloadFactory>
    {
        TRoot childBuilder = TRoot.CreateBuilder();
        rootConfiguration(childBuilder);
        IClasslessQdisc<THandle> qdisc = childBuilder.Build(rootHandle);
        WorkloadScheduler scheduler = _context.ServiceProviderFactory is null
            ? new WorkloadScheduler(qdisc, _context.MaximumConcurrency)
            : new WorkloadSchedulerWithDI(qdisc, _context.MaximumConcurrency, _context.ServiceProviderFactory);
        qdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPoolManager? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPoolManager(_context.PoolSize);
        }
        return TWorkloadFactory.Create(qdisc, pool, _context.ContextOptions);
    }

    private protected TWorkloadFactory UseClassfulRootCore<TWorkloadFactory, TRoot>(THandle rootHandle, Action<ClassfulQdiscBuilder<THandle, TPredicateBuilder, TRoot>> rootClassConfiguration)
        where TRoot : ClassfulQdiscBuilder<TRoot>, IClassfulQdiscBuilder<TRoot>
        where TWorkloadFactory : AbstractClassfulWorkloadFactory<THandle>, IWorkloadFactory<THandle, TWorkloadFactory>
    {
        ClassfulQdiscBuilder<THandle, TPredicateBuilder, TRoot> rootClassBuilder = new(rootHandle);
        rootClassConfiguration(rootClassBuilder);
        IClassfulQdisc<THandle> rootQdisc = rootClassBuilder.Build();

        WorkloadScheduler scheduler = _context.ServiceProviderFactory is null
            ? new WorkloadScheduler(rootQdisc, _context.MaximumConcurrency)
            : new WorkloadSchedulerWithDI(rootQdisc, _context.MaximumConcurrency, _context.ServiceProviderFactory);
        rootQdisc.InternalInitialize(scheduler);
        AnonymousWorkloadPoolManager? pool = null;
        if (_context.UsePooling)
        {
            pool = new AnonymousWorkloadPoolManager(_context.PoolSize);
        }
        return TWorkloadFactory.Create(rootQdisc, pool, _context.ContextOptions);
    }
}

public class WorkloadFactoryBuilder<THandle, TPredicateBuilder> : WorkloadFactoryBuilderBase<THandle, TPredicateBuilder, WorkloadFactoryBuilder<THandle, TPredicateBuilder>> 
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
{
    internal WorkloadFactoryBuilder(QdiscBuilderContext context) : base(context) => Pass();

    internal WorkloadFactoryBuilder() => Pass();

    public WorkloadFactoryBuilder<THandle, TNewPredicateBuilder> UsePredicateBuilder<TNewPredicateBuilder>()
        where TNewPredicateBuilder : IPredicateBuilder, new() => new(_context);

    public WorkloadFactoryBuilderWithDI<THandle, TPredicateBuilder> UseDependencyInjection(Action<WorkloadServiceProviderBuilder> configurationAction) =>
        UseDependencyInjection<SimpleWorkloadServiceProviderFactory>(configurationAction);

    public WorkloadFactoryBuilderWithDI<THandle, TPredicateBuilder> UseDependencyInjection<TServiceProviderFactory>(Action<WorkloadServiceProviderBuilder> configurationAction)
        where TServiceProviderFactory : class, IWorkloadServiceProviderFactory, new()
    {
        IWorkloadServiceProviderFactory factoryProvider = new TServiceProviderFactory();
        WorkloadServiceProviderBuilder builder = new(factoryProvider);
        configurationAction.Invoke(builder);
        _context.ServiceProviderFactory = builder.Build();
        return new WorkloadFactoryBuilderWithDI<THandle, TPredicateBuilder>(_context);
    }

    public ClasslessWorkloadFactory<THandle> UseClasslessRoot<TRoot>(THandle rootHandle)
        where TRoot : ClasslessQdiscBuilder<TRoot>, IClasslessQdiscBuilder<TRoot> =>
            UseClasslessRootCore<ClasslessWorkloadFactory<THandle>, TRoot>(rootHandle, Pass);

    public ClasslessWorkloadFactory<THandle> UseClasslessRoot<TRoot>(THandle rootHandle, Action<TRoot> rootConfiguration)
        where TRoot : ClasslessQdiscBuilder<TRoot>, IClasslessQdiscBuilder<TRoot> =>
            UseClasslessRootCore<ClasslessWorkloadFactory<THandle>, TRoot>(rootHandle, rootConfiguration);

    public ClassfulWorkloadFactory<THandle> UseClassfulRoot<TRoot>(THandle rootHandle)
        where TRoot : ClassfulQdiscBuilder<TRoot>, IClassfulQdiscBuilder<TRoot> => 
            UseClassfulRootCore<ClassfulWorkloadFactory<THandle>, TRoot>(rootHandle, Pass);

    public ClassfulWorkloadFactory<THandle> UseClassfulRoot<TRoot>(THandle rootHandle, Action<ClassfulQdiscBuilder<THandle, TPredicateBuilder, TRoot>> rootClassConfiguration)
        where TRoot : ClassfulQdiscBuilder<TRoot>, IClassfulQdiscBuilder<TRoot> =>
            UseClassfulRootCore<ClassfulWorkloadFactory<THandle>, TRoot>(rootHandle, rootClassConfiguration);
}

public class WorkloadFactoryBuilderWithDI<THandle, TPredicateBuilder> : WorkloadFactoryBuilderBase<THandle, TPredicateBuilder, WorkloadFactoryBuilderWithDI<THandle, TPredicateBuilder>>
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
{
    internal WorkloadFactoryBuilderWithDI(QdiscBuilderContext context) : base(context) => Pass();

    public WorkloadFactoryBuilder<THandle, TNewPredicateBuilder> UsePredicateBuilder<TNewPredicateBuilder>()
        where TNewPredicateBuilder : IPredicateBuilder, new() => new(_context);

    public ClasslessWorkloadFactoryWithDI<THandle> UseClasslessRoot<TRoot>(THandle rootHandle)
        where TRoot : ClasslessQdiscBuilder<TRoot>, IClasslessQdiscBuilder<TRoot> =>
            UseClasslessRootCore<ClasslessWorkloadFactoryWithDI<THandle>, TRoot>(rootHandle, Pass);

    public ClasslessWorkloadFactoryWithDI<THandle> UseClasslessRoot<TRoot>(THandle rootHandle, Action<TRoot> rootConfiguration)
        where TRoot : ClasslessQdiscBuilder<TRoot>, IClasslessQdiscBuilder<TRoot> =>
            UseClasslessRootCore<ClasslessWorkloadFactoryWithDI<THandle>, TRoot>(rootHandle, rootConfiguration);

    public ClassfulWorkloadFactoryWithDI<THandle> UseClassfulRoot<TRoot>(THandle rootHandle)
        where TRoot : ClassfulQdiscBuilder<TRoot>, IClassfulQdiscBuilder<TRoot> =>
            UseClassfulRootCore<ClassfulWorkloadFactoryWithDI<THandle>, TRoot>(rootHandle, Pass);

    public ClassfulWorkloadFactoryWithDI<THandle> UseClassfulRoot<TRoot>(THandle rootHandle, Action<ClassfulQdiscBuilder<THandle, TPredicateBuilder, TRoot>> rootClassConfiguration)
        where TRoot : ClassfulQdiscBuilder<TRoot>, IClassfulQdiscBuilder<TRoot> =>
            UseClassfulRootCore<ClassfulWorkloadFactoryWithDI<THandle>, TRoot>(rootHandle, rootClassConfiguration);
}