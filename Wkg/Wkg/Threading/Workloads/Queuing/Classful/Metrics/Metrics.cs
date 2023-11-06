using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classful;

namespace Wkg.Threading.Workloads.Queuing.Classful.Metrics;

public class Metrics : ClassfulQdiscBuilder<Metrics>, IClassfulQdiscBuilder<Metrics>
{
    private readonly IQdiscBuilderContext _context;
    private int _maxSampleCount = -1;
    private bool _usePreciseMeasurements = false;

    private Metrics(IQdiscBuilderContext context) => _context = context;

    public static Metrics CreateBuilder(IQdiscBuilderContext context) => new(context);

    /// <summary>
    /// Sets the maximum number of execution time measurements to take per payload action until the average execution time is considered stable.
    /// </summary>
    /// <param name="maxSampleCount">The maximum number of execution time measurements to take per payload action until the average execution time is considered stable or <c>-1</c> to continue sampling forever.</param>
    /// <returns>The current <see cref="Metrics"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Metrics UseMaxSampleCount(int maxSampleCount)
    {
        if (maxSampleCount is < 1 and not -1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSampleCount), maxSampleCount, "The maximum sample count must be greater than zero or -1 to continue sampling forever.");
        }
        _maxSampleCount = maxSampleCount;
        return this;
    }

    /// <summary>
    /// Sets whether to use precise measurements or fast measurements.
    /// </summary>
    /// <param name="usePreciseMeasurements">Whether to use precise measurements or fast measurements.</param>
    /// <returns>The current <see cref="Metrics"/> instance.</returns>
    public Metrics UsePreciseMeasurements(bool usePreciseMeasurements = true)
    {
        _usePreciseMeasurements = usePreciseMeasurements;
        return this;
    }

    protected internal override IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate) => 
        new MetricsQdisc<THandle>(handle, _context.MaximumConcurrency, _maxSampleCount, _usePreciseMeasurements);
}
