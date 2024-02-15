using Wkg.Threading.Workloads.Queuing.Classful;

namespace Wkg.Threading.Workloads.Configuration.Classful;

public interface IClassfulQdiscBuilder
{
    internal IClassfulQdisc<THandle> BuildInternal<THandle>(THandle handle, Predicate<object?> predicate) where THandle : unmanaged;
}

public interface IClassfulQdiscBuilder<TSelf> where TSelf : IClassfulQdiscBuilder<TSelf>
{
    static abstract TSelf CreateBuilder(IQdiscBuilderContext context);
}

