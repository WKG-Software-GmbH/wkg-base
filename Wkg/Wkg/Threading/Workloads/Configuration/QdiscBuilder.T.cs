﻿using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.DependencyInjection.Configuration;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Configuration;

public class QdiscBuilder<THandle> where THandle : unmanaged
{
    private readonly QdiscBuilderContext _context;

    internal QdiscBuilder()
    {
        _context = new QdiscBuilderContext();
    }

    public QdiscBuilder<THandle> UseDependencyInjection(Action<WorkloadServiceProviderBuilder> configurationAction) =>
        UseDependencyInjection<WorkloadServiceProviderFactory>(configurationAction);

    public QdiscBuilder<THandle> UseDependencyInjection<TFactoryProvider>(Action<WorkloadServiceProviderBuilder> configurationAction)
        where TFactoryProvider : class, IWorkloadServiceProviderFactory, new()
    {
        IWorkloadServiceProviderFactory factoryProvider = new TFactoryProvider();
        WorkloadServiceProviderBuilder builder = new(factoryProvider);
        configurationAction.Invoke(builder);
        return this;
    }

    public QdiscBuilder<THandle> UseMaximumConcurrency(int maximumConcurrency)
    {
        _context.MaximumConcurrency = maximumConcurrency;
        return this;
    }

    public QdiscBuilder<THandle> UseWorkloadContextOptions(WorkloadContextOptions options)
    {
        _context.ContextOptions = options;
        return this;
    }

    public QdiscBuilder<THandle> FlowExecutionContextToContinuations(bool flowExecutionContext = true)
    {
        _context.ContextOptions = _context.ContextOptions with { FlowExecutionContext = flowExecutionContext };
        return this;
    }

    public QdiscBuilder<THandle> RunContinuationsOnCapturedContext(bool continueOnCapturedContext = true)
    {
        _context.ContextOptions = _context.ContextOptions with { ContinueOnCapturedContext = continueOnCapturedContext };
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

    public ClassfulQdiscBuilderRoot<THandle, TState, TQdisc> UseClassfulRoot<TQdisc, TState>(THandle rootHandle, Predicate<TState> rootPredicate) 
        where TQdisc : class, IClassfulQdisc<THandle, TState, TQdisc>
        where TState : class
    {
        TQdisc qdisc = TQdisc.Create(rootHandle, rootPredicate);
        return new ClassfulQdiscBuilderRoot<THandle, TState, TQdisc>(qdisc, _context);
    }
}
