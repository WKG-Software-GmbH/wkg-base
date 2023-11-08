using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.EarliestDueDate;

public class EarliestDueDate : ClassfulQdiscBuilder<EarliestDueDate>, IClassfulQdiscBuilder<EarliestDueDate>
{
    private readonly IQdiscBuilderContext _context;
    private IClasslessQdiscBuilder? _localQueueBuilder;

    private EarliestDueDate(IQdiscBuilderContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public static EarliestDueDate CreateBuilder(IQdiscBuilderContext context) => new(context);

    public EarliestDueDate WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public EarliestDueDate WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private EarliestDueDate WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
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
    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate)
    {
        _localQueueBuilder ??= Fifo.CreateBuilder(_context);
        // todo: fast vs precise time measurements
        // todo: number of measurement samples
        return new EarliestDueDateQdisc<THandle>(handle, predicate, _localQueueBuilder, _context.MaximumConcurrency);
    }
}
