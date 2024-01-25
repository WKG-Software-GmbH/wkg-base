using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;
using Wkg.Threading.Workloads.Queuing.Classless.Random;

namespace Wkg.Threading.Workloads.Queuing.Classless.WorkStealing;

/// <summary>
/// A qdisc that uses work stealing to optimize for throughput.
/// </summary>
public sealed class WorkStealing : ClasslessQdiscBuilder<WorkStealing>, IClasslessQdiscBuilder<WorkStealing>
{
    public static WorkStealing CreateBuilder(IQdiscBuilderContext context) => new();

    protected override IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate) => 
        new WorkStealingQdisc<THandle>(handle, predicate);
}
