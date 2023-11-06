using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Configuration.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classless.Fifo;

/// <summary>
/// A qdisc that implements the First-In-First-Out (FIFO) scheduling algorithm.
/// </summary>
public sealed class Fifo : ClasslessQdiscBuilder<Fifo>, IClasslessQdiscBuilder<Fifo>
{
    public static Fifo CreateBuilder(IQdiscBuilderContext context) => new();

    protected override IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle) => 
        new FifoQdisc<THandle>(handle);
}
