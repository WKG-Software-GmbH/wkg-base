using System.Diagnostics;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Scheduling;

using CommonFlags = WorkloadStatus.CommonFlags;

internal class WorkloadSchedulerWithDI(IQdisc rootQdisc, int maximumConcurrencyLevel, IWorkloadServiceProviderFactory serviceProviderFactory) 
    : WorkloadScheduler(rootQdisc, maximumConcurrencyLevel)
{
    private readonly IWorkloadServiceProviderFactory _serviceProviderFactory = serviceProviderFactory;

    protected override void WorkerLoop(object? state)
    {
        int workerId = (int)state!;
        DebugLog.WriteInfo($"Started worker {workerId}", LogWriter.Blocking);

        using IWorkloadServiceProvider serviceProvider = _serviceProviderFactory.GetInstance();
        bool previousExecutionFailed = false;
        // check for disposal before and after each dequeue (volatile read)
        AbstractWorkloadBase? workload = null;
        int previousWorkerId = workerId;
        while (!_disposed && TryDequeueOrExitSafely(ref workerId, previousExecutionFailed, out workload) && !_disposed)
        {
            previousWorkerId = workerId;
            workload.RegisterServiceProvider(serviceProvider);
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.Status.IsOneOf(CommonFlags.Completed));
            workload.InternalRunContinuations(workerId);
        }
        OnWorkerTerminated(ref workerId, previousWorkerId, workload);
    }
}
