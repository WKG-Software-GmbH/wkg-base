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
        Throw.ArgumentOutOfRangeException.IfNotInRange(capacity, 1, ushort.MaxValue, nameof(capacity));
        if (_capacity != -1)
        {
            throw new InvalidOperationException("Capacity was already specified.");
        }

        _capacity = capacity;
        return this;
    }

    protected override IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        if (_capacity == -1)
        {
            throw new InvalidOperationException("No capacity was specified.");
        }

        return new ConstrainedLifoQdisc<THandle>(handle, predicate, _capacity);
    }
}
