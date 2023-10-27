using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads;

internal interface IWorkload
{
    bool TryInternalBindQdisc(IQdisc qdisc);
}
