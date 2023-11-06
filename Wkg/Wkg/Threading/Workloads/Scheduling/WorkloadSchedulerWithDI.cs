using System.Diagnostics;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.DependencyInjection;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads.Scheduling;

using CommonFlags = WorkloadStatus.CommonFlags;

internal class WorkloadSchedulerWithDI : WorkloadScheduler
{
    private readonly IWorkloadServiceProviderFactory _serviceProviderFactory;

    public WorkloadSchedulerWithDI(IQdisc rootQdisc, int maximumConcurrencyLevel, IWorkloadServiceProviderFactory serviceProviderFactory) 
        : base(rootQdisc, maximumConcurrencyLevel)
    {
        _serviceProviderFactory = serviceProviderFactory;
    }

    protected override void WorkerLoop(object? state)
    {
        int workerId = (int)state!;
        DebugLog.WriteInfo($"Started worker {workerId}", LogWriter.Blocking);

        using IWorkloadServiceProvider serviceProvider = _serviceProviderFactory.GetInstance();
        bool previousExecutionFailed = false;
        while (TryDequeueOrExitSafely(ref workerId, previousExecutionFailed, out AbstractWorkloadBase? workload))
        {
            workload.RegisterServiceProvider(serviceProvider);
            previousExecutionFailed = !workload.TryRunSynchronously();
            Debug.Assert(workload.Status.IsOneOf(CommonFlags.Completed));
            workload.InternalRunContinuations(workerId);
        }
        DebugLog.WriteInfo($"Terminated worker with previous ID {workerId}.", LogWriter.Blocking);
    }
}
