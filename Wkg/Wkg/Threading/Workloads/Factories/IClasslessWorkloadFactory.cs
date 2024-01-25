using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Factories;

public interface IClasslessWorkloadFactory<THandle> where THandle : unmanaged
{
    IClassifyingQdisc<THandle> Root { get; }
}
