using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.Fair;

public class Fair : ClassfulQdiscBuilder<Fair>, IClassfulQdiscBuilder<Fair>
{
    private readonly IQdiscBuilderContext _context;
    private readonly FairQdiscParams _params;

    private Fair(IQdiscBuilderContext context)
    {
        _context = context;
        _params = new FairQdiscParams()
        {
            Inner = null,
            ConcurrencyLevel = context.MaximumConcurrency,
            ExpectedNumberOfDistinctPayloads = 32,
            MeasurementSampleLimit = -1,
            PreferPreciseMeasurements = false,
            PreferredFairness = PreferredFairness.ShortTerm,
            SchedulerTimeModel = VirtualTimeModel.WorstCase,
            ExecutionTimeModel = VirtualTimeModel.Average
        };
    }

    /// <inheritdoc/>
    public static Fair CreateBuilder(IQdiscBuilderContext context) => new(context);

    /// <summary>
    /// Sets the assumed maximum number of distinct payloads that will be enqueued. This value is used to pre-allocate
    /// the internal data structures of the queue. If the number of distinct payloads exceeds this value, the queue
    /// will still work, but it may have to resize its internal data structures, which may cause a performance hit.
    /// </summary>
    /// <remarks>
    /// A payload is considered distinct if the underlying action points to a different implementation at runtime.
    /// </remarks>
    /// <param name="expectedNumberOfDistinctPayloads">The estimated maximum number of distinct payloads that will be enqueued.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair AssumeMaximimNumberOfDistinctPayloads(int expectedNumberOfDistinctPayloads)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(expectedNumberOfDistinctPayloads, nameof(expectedNumberOfDistinctPayloads));

        _params.ExpectedNumberOfDistinctPayloads = expectedNumberOfDistinctPayloads;
        return this;
    }

    /// <summary>
    /// Whether to prefer precise measurements over performance.
    /// </summary>
    /// <remarks>
    /// Precise measurements may be preferable in scenarios where workloads are very short-lived and fairness is important.
    /// </remarks>
    /// <param name="usePreciseMeasurements">Whether to prefer precise measurements over performance.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair UsePreciseMeasurements(bool usePreciseMeasurements = true)
    {
        _params.PreferPreciseMeasurements = usePreciseMeasurements;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of samples to take when dynamically measuring the execution time of workloads.
    /// </summary>
    /// <param name="measurementSampleLimit">The maximum number of samples to take when dynamically measuring the execution time of workloads, or <c>-1</c> for continuous sampling.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair SetMeasurementSampleLimit(int measurementSampleLimit)
    {
        Throw.ArgumentOutOfRangeException.IfLessThan(measurementSampleLimit, -1, nameof(measurementSampleLimit));
        Throw.ArgumentOutOfRangeException.IfZero(measurementSampleLimit, nameof(measurementSampleLimit));

        _params.MeasurementSampleLimit = measurementSampleLimit;
        return this;
    }

    /// <summary>
    /// Sets the preferred fairness model.
    /// </summary>
    /// <param name="preferredFairness">The preferred fairness model.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair PreferFairness(PreferredFairness preferredFairness)
    {
        _params.PreferredFairness = preferredFairness;
        return this;
    }

    /// <summary>
    /// Specifies the virtual time model to use for scheduling. Scheduling refers to the process of choosing the next
    /// workload to execute from the set of workloads that are due to be dequeued from child queues.
    /// </summary>
    /// <param name="timeModel">The virtual time model to use for scheduling.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair UseSchedulerTimeModel(VirtualTimeModel timeModel)
    {
        _params.SchedulerTimeModel = timeModel;
        return this;
    }

    /// <summary>
    /// Specifies the virtual time model to use to estimate the execution time of workloads. This influences the
    /// penalty that is applied to a queue after a workload has been dequeued from it.
    /// </summary>
    /// <param name="timeModel">The virtual time model to use to estimate the execution time of workloads.</param>
    /// <returns>The current <see cref="Fair"/> instance.</returns>
    public Fair UseExecutionTimeModel(VirtualTimeModel timeModel)
    {
        _params.ExecutionTimeModel = timeModel;
        return this;
    }

    public Fair WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public Fair WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private Fair WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue>
    {
        if (_params.Inner is not null)
        {
            throw new InvalidOperationException("Local queue has already been configured.");
        }

        TLocalQueue localQueueBuilder = TLocalQueue.CreateBuilder(_context);
        configureLocalQueue?.Invoke(localQueueBuilder);
        _params.Inner = localQueueBuilder;

        return this;
    }

    /// <inheritdoc/>
    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate)
    {
        _params.Inner ??= Fifo.CreateBuilder(_context);
        _params.Predicate = predicate;
        return new FairQdisc<THandle>(handle, _params);
    }
}
