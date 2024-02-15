using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing.Classless;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classless;

public interface IClasslessQdiscBuilder
{
    IClassifyingQdisc<THandle> Build<THandle>(THandle handle, Predicate<object?>? predicate) where THandle : unmanaged;

    IClassifyingQdisc<THandle> BuildUnsafe<THandle>(THandle handle = default, Predicate<object?>? predicate = null) where THandle : unmanaged;
}

public interface IClasslessQdiscBuilder<TSelf> : IClasslessQdiscBuilder where TSelf : ClasslessQdiscBuilder<TSelf>, IClasslessQdiscBuilder<TSelf>
{
    static abstract TSelf CreateBuilder(IQdiscBuilderContext context);
}

public abstract class ClasslessQdiscBuilder<TSelf> : IClasslessQdiscBuilder where TSelf : ClasslessQdiscBuilder<TSelf>, IClasslessQdiscBuilder<TSelf>
{
    protected abstract IClassifyingQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate) where THandle : unmanaged;

    IClassifyingQdisc<THandle> IClasslessQdiscBuilder.BuildUnsafe<THandle>(THandle handle, Predicate<object?>? predicate) => 
        BuildInternal(handle, predicate);

    IClassifyingQdisc<THandle> IClasslessQdiscBuilder.Build<THandle>(THandle handle, Predicate<object?>? predicate)
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(handle);

        return BuildInternal(handle, predicate);
    }
}