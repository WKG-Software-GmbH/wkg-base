using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.ConstrainedLifo;

public sealed class ConstrainedLifo : ClasslessQdiscBuilder<ConstrainedLifo>, IClasslessQdiscBuilder<ConstrainedLifo>
{
    private int _capacity = -1;

    public static ConstrainedLifo CreateBuilder(IQdiscBuilderContext context) => new();

    public ConstrainedLifo WithCapacity(int capacity)
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

        return new ConstrainedLifoQdisc<THandle>(handle, _capacity);
    }
}
