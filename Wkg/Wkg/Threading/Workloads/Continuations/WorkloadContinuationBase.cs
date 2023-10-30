using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wkg.Threading.Workloads.Continuations;

internal abstract class WorkloadContinuationBase : IWorkloadContinuation
{
    protected readonly Action _continuation;

    protected WorkloadContinuationBase(Action continuation)
    {
        _continuation = continuation;
    }

    public abstract void Invoke();
}
