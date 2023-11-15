using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classless;

public interface IClasslessQdiscBuilder
{
    IClasslessQdisc<THandle> Build<THandle>(THandle handle) where THandle : unmanaged;

    IClasslessQdisc<THandle> BuildUnsafe<THandle>(THandle handle = default) where THandle : unmanaged;
}

public interface IClasslessQdiscBuilder<TSelf> : IClasslessQdiscBuilder where TSelf : ClasslessQdiscBuilder<TSelf>, IClasslessQdiscBuilder<TSelf>
{
    static abstract TSelf CreateBuilder(IQdiscBuilderContext context);
}

public abstract class ClasslessQdiscBuilder<TSelf> : IClasslessQdiscBuilder where TSelf : ClasslessQdiscBuilder<TSelf>, IClasslessQdiscBuilder<TSelf>
{
    protected abstract IClasslessQdisc<THandle> BuildInternal<THandle>(THandle handle) where THandle : unmanaged;

    IClasslessQdisc<THandle> IClasslessQdiscBuilder.BuildUnsafe<THandle>(THandle handle) => BuildInternal(handle);

    IClasslessQdisc<THandle> IClasslessQdiscBuilder.Build<THandle>(THandle handle)
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(handle);

        return BuildInternal(handle);
    }
}