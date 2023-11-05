using Wkg.Common.ThrowHelpers;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Configuration;

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
        Throw.WorkloadSchedulingException.IfHandleIsDefault(handle);

        return BuildInternal(handle);
    }
}