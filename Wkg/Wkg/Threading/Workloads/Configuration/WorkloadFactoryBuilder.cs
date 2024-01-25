using Wkg.Threading.Workloads.Queuing.Classful.Classification;

namespace Wkg.Threading.Workloads.Configuration;

public static class WorkloadFactoryBuilder
{
    public static WorkloadFactoryBuilder<THandle, SimplePredicateBuilder> Create<THandle>() where THandle : unmanaged => new();
}
