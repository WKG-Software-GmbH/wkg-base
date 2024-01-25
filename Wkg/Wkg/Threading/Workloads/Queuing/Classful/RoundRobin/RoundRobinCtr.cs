﻿using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
[Obsolete($"This qdisc exhibits less performance than {nameof(RoundRobinBitmap)} and should not be used. It's also overly complex and difficult to maintain.")]
public sealed class RoundRobinCtr : ClassfulQdiscBuilder<RoundRobinCtr>, IClassfulQdiscBuilder<RoundRobinCtr>
{
    private readonly IQdiscBuilderContext _context;
    private IClasslessQdiscBuilder? _localQueueBuilder;

    private RoundRobinCtr(IQdiscBuilderContext context) => _context = context;

    public static RoundRobinCtr CreateBuilder(IQdiscBuilderContext context) => new(context);

    public RoundRobinCtr WithLocalQueue<TLocalQueue>() 
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public RoundRobinCtr WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue) 
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private RoundRobinCtr WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue>
    {
        if (_localQueueBuilder is not null)
        {
            throw new InvalidOperationException("Local queue has already been configured.");
        }

        TLocalQueue localQueueBuilder = TLocalQueue.CreateBuilder(_context);
        configureLocalQueue?.Invoke(localQueueBuilder);
        _localQueueBuilder = localQueueBuilder;

        return this;
    }

    /// <inheritdoc/>
    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        _localQueueBuilder ??= Fifo.CreateBuilder(_context);
        return new RoundRobinCtrQdisc<THandle>(handle, predicate, _localQueueBuilder, _context.MaximumConcurrency);
    }
}