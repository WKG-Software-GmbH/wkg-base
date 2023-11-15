using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classful.Custom;

public abstract class CustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf> : ICustomClassfulQdiscBuilder<THandle>
    where THandle : unmanaged
    where TSelf : CustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf>, ICustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf>
    where TPredicateBuilder : IPredicateBuilder, new()
{
    protected readonly THandle _handle;

    protected CustomClassfulQdiscBuilder(THandle handle)
    {
        _handle = handle;
    }

    protected abstract IClassfulQdisc<THandle> BuildInternal(THandle handle);

    internal IClassfulQdisc<THandle> Build()
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(_handle);

        return BuildInternal(_handle);
    }

    IClassfulQdisc<THandle> ICustomClassfulQdiscBuilder<THandle>.Build() => Build();
}