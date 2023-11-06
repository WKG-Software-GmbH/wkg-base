using Wkg.Threading.Workloads.Queuing.Classful;
using Wkg.Threading.Workloads.Queuing.Classful.Classification;

namespace Wkg.Threading.Workloads.Configuration.Classful.Custom;

public interface ICustomClassfulQdiscBuilder<THandle> where THandle : unmanaged
{
    internal IClassfulQdisc<THandle> Build();
}

public interface ICustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf>
    where THandle : unmanaged
    where TPredicateBuilder : IPredicateBuilder, new()
    where TSelf : CustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf>, ICustomClassfulQdiscBuilder<THandle, TPredicateBuilder, TSelf>
{
    static abstract TSelf CreateBuilder(THandle handle, IQdiscBuilderContext context);
}
