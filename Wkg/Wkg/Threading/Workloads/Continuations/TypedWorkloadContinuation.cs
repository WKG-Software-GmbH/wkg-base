using System.Diagnostics;

namespace Wkg.Threading.Workloads.Continuations;

internal abstract class TypedWorkloadContinuation<TWorkload> : IWorkloadContinuation
    where TWorkload : AbstractWorkloadBase
{
    public void Invoke(AbstractWorkloadBase workload)
    {
        Debug.Assert(workload is TWorkload);
        InvokeInternal(ReinterpretCast<AbstractWorkloadBase, TWorkload>(workload));
    }

    public void InvokeInline(AbstractWorkloadBase workload)
    {
        Debug.Assert(workload is TWorkload);
        InvokeInternal(ReinterpretCast<AbstractWorkloadBase, TWorkload>(workload));
    }

    protected abstract void InvokeInternal(TWorkload workload);
}
