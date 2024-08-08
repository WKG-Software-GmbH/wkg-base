using Wkg.Internals.Diagnostic;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

public class FairQueuing : ClassfulQdiscBuilder<FairQueuing>, IClassfulQdiscBuilder<FairQueuing>
{
    private readonly IQdiscBuilderContext _context;
    private readonly GfqQdiscParams _params;

    private FairQueuing(IQdiscBuilderContext context)
    {
        _context = context;
        _params = new GfqQdiscParams()
        {
            Inner = null,
            ConcurrencyLevel = context.MaximumConcurrency,
            ExpectedNumberOfDistinctPayloads = 32,
            MeasurementSampleLimit = -1,
            PreferPreciseMeasurements = false,
            SchedulingParams = WfqSchedulingParams.Default,
        };
    }

    /// <inheritdoc/>
    public static FairQueuing CreateBuilder(IQdiscBuilderContext context) => new(context);

    /// <summary>
    /// Sets the assumed maximum number of distinct payloads that will be enqueued. This value is used to pre-allocate
    /// the internal data structures of the queue. If the number of distinct payloads exceeds this value, the queue
    /// will still work, but it may have to resize its internal data structures, which may cause a performance hit.
    /// </summary>
    /// <remarks>
    /// A payload is considered distinct if the underlying action points to a different implementation at runtime.
    /// </remarks>
    /// <param name="expectedNumberOfDistinctPayloads">The estimated maximum number of distinct payloads that will be enqueued.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing AssumeMaximimNumberOfDistinctPayloads(int expectedNumberOfDistinctPayloads)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedNumberOfDistinctPayloads, nameof(expectedNumberOfDistinctPayloads));

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
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing UsePreciseMeasurements(bool usePreciseMeasurements = true)
    {
        _params.PreferPreciseMeasurements = usePreciseMeasurements;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of samples to take when dynamically measuring the execution time of workloads.
    /// </summary>
    /// <param name="measurementSampleLimit">The maximum number of samples to take when dynamically measuring the execution time of workloads, or <c>-1</c> for continuous sampling.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing SetMeasurementSampleLimit(int measurementSampleLimit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(measurementSampleLimit, -1, nameof(measurementSampleLimit));
        ArgumentOutOfRangeException.ThrowIfZero(measurementSampleLimit, nameof(measurementSampleLimit));

        _params.MeasurementSampleLimit = measurementSampleLimit;
        return this;
    }

    /// <summary>
    /// Sets the preferred fairness model.
    /// </summary>
    /// <param name="preferredFairness">The preferred fairness model.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing PreferFairness(PreferredFairness preferredFairness)
    {
        _params.PreferredFairness = preferredFairness;
        return this;
    }

    /// <summary>
    /// Specifies the virtual time model to use for scheduling. Scheduling refers to the process of choosing the next
    /// workload to execute from the set of workloads that are due to be dequeued from child queues.
    /// </summary>
    /// <param name="timeModel">The virtual time model to use for scheduling.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing UseSchedulerTimeModel(VirtualTimeModel timeModel)
    {
        _params.SchedulerTimeModel = timeModel;
        return this;
    }

    /// <summary>
    /// Specifies the virtual time model to use to estimate the execution time of workloads. This influences the
    /// penalty that is applied to a queue after a workload has been dequeued from it.
    /// </summary>
    /// <param name="timeModel">The virtual time model to use to estimate the execution time of workloads.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public FairQueuing UseExecutionTimeModel(VirtualTimeModel timeModel)
    {
        _params.ExecutionTimeModel = timeModel;
        return this;
    }

    public FairQueuing UseVirtualTimeFunction<TFunction>() where TFunction : IVirtualTimeFunction, new()
    {
        if (_params.HasVirtualTimeFunction)
        {
            throw new InvalidOperationException("Virtual time function has already been configured.");
        }
        if (_params.SchedulingParams != WfqSchedulingParams.Default)
        {
            DebugLog.WriteWarning($"Virtual time function is being configured, but scheduling parameters have already been configured. The virtual time function will override the scheduling parameters.");
        }
        TFunction function = new();
        _params.VirtualFinishTimeFunction = function.CalculateVirtualFinishTime;
        _params.VirtualExecutionTimeFunction = function.CalculateVirtualExecutionTime;
        _params.VirtualAccumulatedFinishTimeFunction = function.CalculateVirtualAccumulatedFinishTime;
        return this;
    }

    public FairQueuing WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public FairQueuing WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private FairQueuing WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
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
    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        _params.Inner ??= Fifo.CreateBuilder(_context);
        _params.Predicate = predicate;
        if (!_params.HasVirtualTimeFunction)
        {
            ParameterizedGfqVirtualTimeFunction virtualTimeFunction = new(_params.SchedulingParams);
            _params.VirtualFinishTimeFunction = virtualTimeFunction.CalculateVirtualFinishTime;
            _params.VirtualExecutionTimeFunction = virtualTimeFunction.CalculateVirtualExecutionTime;
            _params.VirtualAccumulatedFinishTimeFunction = virtualTimeFunction.CalculateVirtualAccumulatedFinishTime;
        }
        return new GfqQdisc<THandle>(handle, _params);
    }
}
