using Wkg.Threading.Workloads.Queuing.Classful;

namespace Wkg.Threading.Workloads.Configuration.Classful.Custom;

public interface ICustomClassfulQdiscBuilder<THandle> where THandle : unmanaged
{
    internal IClassfulQdisc<THandle> Build();
}

public interface ICustomClassfulQdiscBuilder<THandle, TSelf>
    where THandle : unmanaged
    where TSelf : CustomClassfulQdiscBuilder<THandle, TSelf>, ICustomClassfulQdiscBuilder<THandle, TSelf>
{
    static abstract TSelf CreateBuilder(THandle handle, IQdiscBuilderContext context);
}
