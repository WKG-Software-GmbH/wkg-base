using Wkg.Threading.Workloads.Exceptions;
using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Scheduling;

namespace Wkg.Threading.Workloads.Configuration.Classful.Custom;

public abstract class CustomClassfulQdiscBuilder<THandle, TSelf> : ICustomClassfulQdiscBuilder<THandle>
    where THandle : unmanaged
    where TSelf : CustomClassfulQdiscBuilder<THandle, TSelf>, ICustomClassfulQdiscBuilder<THandle, TSelf>
{
    private readonly THandle _handle;
    protected readonly IQdiscBuilderContext _context;

    protected CustomClassfulQdiscBuilder(THandle handle, IQdiscBuilderContext context)
    {
        _handle = handle;
        _context = context;
    }

    protected abstract IClassfulQdisc<THandle> BuildInternal(THandle handle);

    internal IClassfulQdisc<THandle> Build()
    {
        WorkloadSchedulingException.ThrowIfHandleIsDefault(_handle);

        return BuildInternal(_handle);
    }

    IClassfulQdisc<THandle> ICustomClassfulQdiscBuilder<THandle>.Build() => Build();
}