using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public partial class Workload
{
    public static Task WhenAll(params AwaitableWorkload[] workloads) => Task.Run(() => 
    {
        foreach (AwaitableWorkload workload in workloads)
        {
            // TODO: This is a temporary solution. actually schedule continuations for this...
            workload.Wait();
        }
    });
}
