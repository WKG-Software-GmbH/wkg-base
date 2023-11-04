using Wkg.Threading.Workloads.Configuration;

namespace Wkg.Threading.Workloads.Queuing.Classful.RoundRobin;

/// <summary>
/// A classful qdisc that implements the Round Robin scheduling algorithm to dequeue workloads from its children.
/// </summary>
public sealed class RoundRobin : ClassfulQdiscBuilder<RoundRobin>, IClassfulQdiscBuilder<RoundRobin>
{
    private IClasslessQdiscBuilder? _localQueueBuilder;

    public static RoundRobin CreateBuilder() => new();

    public RoundRobin WithLocalQueue<TLocalQueue>() 
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public RoundRobin WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue) 
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private RoundRobin WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue>
    {
        if (_localQueueBuilder is not null)
        {
            throw new InvalidOperationException("Local queue has already been configured.");
        }

        TLocalQueue localQueueBuilder = TLocalQueue.CreateBuilder();
        configureLocalQueue?.Invoke(localQueueBuilder);
        _localQueueBuilder = localQueueBuilder;

        return this;
    }

    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate) => 
        new RoundRobinQdisc<THandle>(handle, predicate, _localQueueBuilder);
}