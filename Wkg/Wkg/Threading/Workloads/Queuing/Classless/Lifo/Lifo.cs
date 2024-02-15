using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.Lifo;

/// <summary>
/// A qdisc that implements the Last-In-First-Out (LIFO) scheduling algorithm.
/// </summary>
public sealed class Lifo : ClasslessQdiscBuilder<Lifo>, IClasslessQdiscBuilder<Lifo>
{
    public static Lifo CreateBuilder(IQdiscBuilderContext context) => new();

    protected override IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate) => 
        new LifoQdisc<THandle>(handle, predicate);
}
