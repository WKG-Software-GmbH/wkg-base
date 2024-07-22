using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classful.Custom;

public abstract class CustomClassfulQdiscBuilder<THandle, TSelf>(THandle handle, IQdiscBuilderContext context) : ICustomClassfulQdiscBuilder<THandle>
    where THandle : unmanaged
    where TSelf : CustomClassfulQdiscBuilder<THandle, TSelf>, ICustomClassfulQdiscBuilder<THandle, TSelf>
{
    protected readonly IQdiscBuilderContext _context = context;

    protected abstract IClassfulQdisc<THandle> BuildInternal(THandle handle);

    internal IClassfulQdisc<THandle> Build()
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(handle);

        return BuildInternal(handle);
    }

    IClassfulQdisc<THandle> ICustomClassfulQdiscBuilder<THandle>.Build() => Build();
}