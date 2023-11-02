namespace Wkg.Threading.Workloads.Configuration;

public static class WorkloadFactoryBuilder
{
    public static WorkloadFactoryBuilder<THandle> Create<THandle>() where THandle : unmanaged => new();
}
