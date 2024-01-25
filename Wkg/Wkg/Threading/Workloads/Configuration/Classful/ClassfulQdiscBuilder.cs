using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classful;

public abstract class ClassfulQdiscBuilder<TSelf> : IClassfulQdiscBuilder where TSelf : ClassfulQdiscBuilder<TSelf>, IClassfulQdiscBuilder<TSelf>
{
    internal protected abstract IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate) where THandle : unmanaged;

    internal IClassfulQdisc<THandle> Build<THandle>(THandle handle, Predicate<object?>? predicate) where THandle : unmanaged
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(handle);

        return BuildInternal(handle, predicate);
    }

    IClassfulQdisc<THandle> IClassfulQdiscBuilder.BuildInternal<THandle>(THandle handle, Predicate<object?>? predicate) => BuildInternal(handle, predicate);
}
