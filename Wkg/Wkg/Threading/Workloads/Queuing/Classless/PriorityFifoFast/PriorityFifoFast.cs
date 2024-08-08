using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

public class PriorityFifoFast : ClasslessQdiscBuilder<PriorityFifoFast>, IClasslessQdiscBuilder<PriorityFifoFast>
{
    private object? _bandHandles;
    private int _bandCount = -1;
    private int _defaultBand = -1;
    private Func<object?, int>? _bandSelector;

    private PriorityFifoFast() => Pass();

    public static PriorityFifoFast CreateBuilder(IQdiscBuilderContext context) => new();

    public PriorityFifoFast WithBandHandles<THandle>(params THandle[] bandHandles) where THandle : unmanaged
    {
        _bandHandles = bandHandles;
        return this;
    }

    public PriorityFifoFast WithBandCount(int bandCount)
    {
        _bandCount = bandCount;
        return this;
    }

    public PriorityFifoFast WithDefaultBand(int defaultBand)
    {
        _defaultBand = defaultBand;
        return this;
    }

    public PriorityFifoFast WithBandSelector(Func<object?, int> bandSelector)
    {
        _bandSelector = bandSelector;
        return this;
    }

    protected override IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        THandle[] bandHandles;
        int bandCount;
        if (_bandHandles is null)
        {
            bandHandles = [];
            bandCount = _bandCount;
            if (bandCount == -1)
            {
                bandCount = 3;
            }
        }
        else if (_bandHandles is THandle[] handles)
        {
            bandHandles = handles;
            if (_bandCount != -1 && bandHandles.Length != _bandCount)
            {
                throw new InvalidOperationException("The configured band handles do not match the configured band count.");
            }
            bandCount = bandHandles.Length;
        }
        else
        {
            throw new InvalidOperationException("The configured band handles are not of the expected type.");
        }
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(bandCount, 1, nameof(bandCount));
        int defaultBand;
        if (_defaultBand == -1)
        {
            defaultBand = bandCount - 1;
        }
        else
        {
            defaultBand = _defaultBand;
        }
        Throw.ArgumentOutOfRangeException.IfNotInRange(defaultBand, 0, bandCount - 1, nameof(defaultBand));
        Func<object?, int> bandSelector = _bandSelector ?? (state => defaultBand);
        return new PriorityFifoFastQdisc<THandle>(handle, bandHandles, bandCount, defaultBand, bandSelector, predicate);
    }
}
