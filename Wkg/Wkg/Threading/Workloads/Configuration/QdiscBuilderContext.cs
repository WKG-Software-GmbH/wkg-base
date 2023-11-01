namespace Wkg.Threading.Workloads.Configuration;

internal class QdiscBuilderContext
{
    public int MaximumConcurrency { get; set; } = 2;

    public int PoolSize { get; set; } = -1;

    public bool UsePooling => PoolSize > 0;

    public WorkloadContextOptions ContextOptions { get; set; } = new();
}