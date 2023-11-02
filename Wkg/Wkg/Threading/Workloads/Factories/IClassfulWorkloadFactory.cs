using Wkg.Threading.Workloads.Queuing.Classful;

namespace Wkg.Threading.Workloads.Factories;

public interface IClassfulWorkloadFactory<THandle> where THandle : unmanaged
{
    IClassfulQdisc<THandle> Root { get; }
}
