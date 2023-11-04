using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Configuration;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedFifo;

public sealed class ConstrainedFifo : ClasslessQdiscBuilder<ConstrainedFifo>, IClasslessQdiscBuilder<ConstrainedFifo>
{
    private int _capacity = -1;

    public static ConstrainedFifo CreateBuilder() => new();

    public ConstrainedFifo WithCapacity(int capacity)
    {
        Throw.ArgumentOutOfRangeException.IfNegativeOrZero(capacity, nameof(capacity));
        Throw.ArgumentOutOfRangeException.IfGreaterThan(capacity, ushort.MaxValue, nameof(capacity));
        if (_capacity != -1)
        {
            throw new InvalidOperationException("Capacity was already specified.");
        }

        _capacity = capacity;
        return this;
    }

    protected override IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle)
    {
        if (_capacity == -1)
        {
            throw new InvalidOperationException("No capacity was specified.");
        }

        return new ConstrainedFifoQdisc<THandle>(handle, _capacity);
    }
}
