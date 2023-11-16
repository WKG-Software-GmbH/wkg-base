using Wkg.Internals.Diagnostic;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;
using Wkg.Threading.Workloads.Configuration.Classful.Custom;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Threading.Workloads.Queuing.Classful.FairQueuing;

public class WeightedFairQueuing<THandle> : CustomClassfulQdiscBuilder<THandle, WeightedFairQueuing<THandle>>, ICustomClassfulQdiscBuilder<THandle, WeightedFairQueuing<THandle>>
    where THandle : unmanaged
{
    private protected readonly WfqQdiscParams _params;
    private readonly List<(IClasslessQdisc<THandle>, Predicate<object?>?, WfqWeight)> _children = [];

    private WeightedFairQueuing(THandle handle, IQdiscBuilderContext context) : base(handle, context)
    {
        _params = new WfqQdiscParams()
        {
            Inner = null,
            ConcurrencyLevel = context.MaximumConcurrency,
            ExpectedNumberOfDistinctPayloads = 32,
            MeasurementSampleLimit = -1,
            PreferPreciseMeasurements = false,
            SchedulingParams = WfqSchedulingParams.Default,
        };
    }

    public static WeightedFairQueuing<THandle> CreateBuilder(THandle handle, IQdiscBuilderContext context) =>
        new(handle, context);

    public WeightedFairQueuing<THandle> WithClassificationPredicate(Predicate<object?> predicate)
    {
        _params.Predicate = predicate;
        return this;
    }

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
    public WeightedFairQueuing<THandle> AssumeMaximimNumberOfDistinctPayloads(int expectedNumberOfDistinctPayloads)
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
    public WeightedFairQueuing<THandle> UsePreciseMeasurements(bool usePreciseMeasurements = true)
    {
        _params.PreferPreciseMeasurements = usePreciseMeasurements;
        return this;
    }

    /// <summary>
    /// Sets the maximum number of samples to take when dynamically measuring the execution time of workloads.
    /// </summary>
    /// <param name="measurementSampleLimit">The maximum number of samples to take when dynamically measuring the execution time of workloads, or <c>-1</c> for continuous sampling.</param>
    /// <returns>The current <see cref="FairQueuing"/> instance.</returns>
    public WeightedFairQueuing<THandle> SetMeasurementSampleLimit(int measurementSampleLimit)
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
    public WeightedFairQueuing<THandle> PreferFairness(PreferredFairness preferredFairness)
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
    public WeightedFairQueuing<THandle> UseSchedulerTimeModel(VirtualTimeModel timeModel)
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
    public WeightedFairQueuing<THandle> UseExecutionTimeModel(VirtualTimeModel timeModel)
    {
        _params.ExecutionTimeModel = timeModel;
        return this;
    }

    public WeightedFairQueuing<THandle> UseVirtualTimeFunction<TFunction>() where TFunction : IVirtualTimeFunction, new()
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

    public WeightedFairQueuing<THandle> WithLocalQueue<TLocalQueue>()
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore<TLocalQueue>(null);

    public WeightedFairQueuing<THandle> WithLocalQueue<TLocalQueue>(Action<TLocalQueue> configureLocalQueue)
        where TLocalQueue : ClasslessQdiscBuilder<TLocalQueue>, IClasslessQdiscBuilder<TLocalQueue> =>
            WithLocalQueueCore(configureLocalQueue);

    private WeightedFairQueuing<THandle> WithLocalQueueCore<TLocalQueue>(Action<TLocalQueue>? configureLocalQueue)
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

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, double workloadSchedulingWeight = 1d, double executionPunishmentFactor = 1d)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, workloadSchedulingWeight, executionPunishmentFactor, null, null);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, 1d, 1d, null, configureChild);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, Action<SimplePredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, 1d, 1d, configureClassification, null);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, Action<SimplePredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, 1d, 1d, configureClassification, configureChild);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, workloadSchedulingWeight, executionPunishmentFactor, null, configureChild);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<SimplePredicateBuilder> configureClassification)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore<TChild>(childHandle, workloadSchedulingWeight, executionPunishmentFactor, configureClassification, null);

    public WeightedFairQueuing<THandle> AddClasslessChild<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<SimplePredicateBuilder> configureClassification, Action<TChild> configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild> => AddClasslessChildCore(childHandle, workloadSchedulingWeight, executionPunishmentFactor, configureClassification, configureChild);

    private WeightedFairQueuing<THandle> AddClasslessChildCore<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<SimplePredicateBuilder>? configureClassification, Action<TChild>? configureChild)
        where TChild : ClasslessQdiscBuilder<TChild>, IClasslessQdiscBuilder<TChild>
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workloadSchedulingWeight, 0d, nameof(workloadSchedulingWeight));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(executionPunishmentFactor, 0d, nameof(executionPunishmentFactor));

        WfqWeight weight = new(executionPunishmentFactor, workloadSchedulingWeight);
        TChild childBuilder = TChild.CreateBuilder(_context);
        if (configureChild is not null)
        {
            configureChild(childBuilder);
        }
        IClasslessQdisc<THandle> qdisc = childBuilder.Build(childHandle);
        Predicate<object?>? predicate = null;
        if (configureClassification is not null)
        {
            SimplePredicateBuilder predicateBuilder = new();
            configureClassification(predicateBuilder);
            predicate = predicateBuilder.Compile();
        }
        _children.Add((qdisc, predicate, weight));
        return this;
    }

    public WeightedFairQueuing<THandle> AddClassfulChild<TChild>(THandle childHandle, double workloadSchedulingWeight = 1d, double executionPunishmentFactor = 1d)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workloadSchedulingWeight, 0d, nameof(workloadSchedulingWeight));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(executionPunishmentFactor, 0d, nameof(executionPunishmentFactor));

        WfqWeight weight = new(executionPunishmentFactor, workloadSchedulingWeight);
        ClassfulBuilder<THandle, SimplePredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add((qdisc, null, weight));
        return this;
    }

    public WeightedFairQueuing<THandle> AddClassfulChild<TChild>(THandle childHandle, Action<TChild> configureChild)
        where TChild : CustomClassfulQdiscBuilder<THandle, TChild>, ICustomClassfulQdiscBuilder<THandle, TChild> =>
        AddClassfulChild(childHandle, 1d, 1d, configureChild);

    public WeightedFairQueuing<THandle> AddClassfulChild<TChild>(THandle childHandle, Action<ClassfulBuilder<THandle, SimplePredicateBuilder, TChild>> configureChild)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild> =>
        AddClassfulChild(childHandle, 1d, 1d, configureChild);

    public WeightedFairQueuing<THandle> AddClassfulChild<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<TChild> configureChild)
        where TChild : CustomClassfulQdiscBuilder<THandle, TChild>, ICustomClassfulQdiscBuilder<THandle, TChild>
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workloadSchedulingWeight, 0d, nameof(workloadSchedulingWeight));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(executionPunishmentFactor, 0d, nameof(executionPunishmentFactor));

        WfqWeight weight = new(executionPunishmentFactor, workloadSchedulingWeight);
        TChild childBuilder = TChild.CreateBuilder(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add((qdisc, null, weight));
        return this;
    }

    public WeightedFairQueuing<THandle> AddClassfulChild<TChild>(THandle childHandle, double workloadSchedulingWeight, double executionPunishmentFactor, Action<ClassfulBuilder<THandle, SimplePredicateBuilder, TChild>> configureChild)
        where TChild : ClassfulQdiscBuilder<TChild>, IClassfulQdiscBuilder<TChild>
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workloadSchedulingWeight, 0d, nameof(workloadSchedulingWeight));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(executionPunishmentFactor, 0d, nameof(executionPunishmentFactor));

        WfqWeight weight = new(executionPunishmentFactor, workloadSchedulingWeight);
        ClassfulBuilder<THandle, SimplePredicateBuilder, TChild> childBuilder = new(childHandle, _context);
        configureChild(childBuilder);
        IClassfulQdisc<THandle> qdisc = childBuilder.Build();
        _children.Add((qdisc, null, weight));
        return this;
    }

    protected override IClassfulQdisc<THandle> BuildInternal(THandle handle)
    {
        _params.Inner ??= Fifo.CreateBuilder(_context);
        _params.Predicate ??= NoMatch;
        if (!_params.HasVirtualTimeFunction)
        {
            ParameterizedWfqVirtualTimeFunction virtualTimeFunction = new(_params.SchedulingParams);
            _params.VirtualFinishTimeFunction = virtualTimeFunction.CalculateVirtualFinishTime;
            _params.VirtualExecutionTimeFunction = virtualTimeFunction.CalculateVirtualExecutionTime;
            _params.VirtualAccumulatedFinishTimeFunction = virtualTimeFunction.CalculateVirtualAccumulatedFinishTime;
        }
        WfqQdisc<THandle> qdisc = new(handle, _params);
        foreach ((IClasslessQdisc<THandle> child, Predicate<object?>? childPredicate, WfqWeight weight) in _children)
        {
            if (child is IClassfulQdisc<THandle> classfulChild)
            {
                qdisc.TryAddChild(classfulChild, weight);
            }
            else if (childPredicate is not null)
            {
                qdisc.TryAddChild(child, weight, childPredicate);
            }
            else
            {
                qdisc.TryAddChild(child, weight);
            }
        }
        return qdisc;
    }

    private static bool NoMatch(object? _) => false;
}