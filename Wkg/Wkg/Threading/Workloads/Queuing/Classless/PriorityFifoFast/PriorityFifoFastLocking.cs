using Wkg.Common.Extensions;
using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.PriorityFifoFast;

public class PriorityFifoFastLocking : ClasslessQdiscBuilder<PriorityFifoFastLocking>, IClasslessQdiscBuilder<PriorityFifoFastLocking>
{
    private object? _bandHandles;
    private int _bandCount = -1;
    private int _defaultBand = -1;
    private Func<object?, int>? _bandSelector;

    private PriorityFifoFastLocking() => Pass();

    public static PriorityFifoFastLocking CreateBuilder(IQdiscBuilderContext context) => new();

    public PriorityFifoFastLocking WithBandHandles<THandle>(params THandle[] bandHandles) where THandle : unmanaged
    {
        _bandHandles = bandHandles;
        return this;
    }

    public PriorityFifoFastLocking WithBandCount(int bandCount)
    {
        _bandCount = bandCount;
        return this;
    }

    public PriorityFifoFastLocking WithDefaultBand(int defaultBand)
    {
        _defaultBand = defaultBand;
        return this;
    }

    public PriorityFifoFastLocking WithBandSelector(Func<object?, int> bandSelector)
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
        return new PriorityFifoFastQdiscLocking<THandle>(handle, bandHandles, bandCount, defaultBand, bandSelector, predicate);
    }
}
