using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Queuing.Classifiers.Internals;

internal interface IChildClassification<THandle> where THandle : unmanaged
{
    IClasslessQdisc<THandle> Qdisc { get; }

    bool TryEnqueue(object? state, AbstractWorkloadBase workload);
}
