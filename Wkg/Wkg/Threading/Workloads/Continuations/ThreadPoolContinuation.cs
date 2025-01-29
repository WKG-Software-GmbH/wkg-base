using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Wkg.Threading.Workloads.Continuations;

internal class ThreadPoolContinuation(IWorkloadContinuation _innerContinuation) : IWorkloadContinuation
{
    public void Invoke(AbstractWorkloadBase workload) => ThreadPool.UnsafeQueueUserWorkItem(InvokeInternal, workload);

    public void InvokeInline(AbstractWorkloadBase workload) => _innerContinuation.InvokeInline(workload);

    private void InvokeInternal(object? workload)
    {
        Debug.Assert(workload is not null);
        _innerContinuation.Invoke(Unsafe.As<AbstractWorkloadBase>(workload));
    }
}
