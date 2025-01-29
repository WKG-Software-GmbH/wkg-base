using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wkg.Threading.Workloads.Continuations;

internal abstract class TypedWorkloadContinuation<TWorkload> : IWorkloadContinuation
    where TWorkload : AbstractWorkloadBase
{
    public void Invoke(AbstractWorkloadBase workload)
    {
        Debug.Assert(workload is TWorkload);
        InvokeInternal(Unsafe.BitCast<AbstractWorkloadBase, TWorkload>(workload));
    }

    public void InvokeInline(AbstractWorkloadBase workload)
    {
        Debug.Assert(workload is TWorkload);
        InvokeInternal(Unsafe.BitCast<AbstractWorkloadBase, TWorkload>(workload));
    }

    protected abstract void InvokeInternal(TWorkload workload);
}
