using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public abstract partial class Workload : AwaitableWorkload
{
    private protected Workload(WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken) => Pass();

    private protected abstract void ExecuteCore();

    private protected override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus)
    {
        // execute the workload
        ExecuteCore();
        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            Volatile.Write(ref _exception, null);
            DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
            return true;
        }
        else if (preTerminationStatus == WorkloadStatus.Canceled)
        {
            SetCanceledResultUnsafe();
            DebugLog.WriteDiagnostic($"{this}: Execution was canceled.", LogWriter.Blocking);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets an awaiter used to await this workload.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not modify or remove this method. It is used by compiler generated code.
    /// </remarks>
    public WorkloadAwaiter GetAwaiter() => new(this);

    private protected override void SetCanceledResultUnsafe() => 
        Volatile.Write(ref _exception, null);

    private protected override void SetFaultedResultUnsafe(Exception ex) => 
        Volatile.Write(ref _exception, ex);

    internal WorkloadResult GetResultUnsafe() => new(Status, Volatile.Read(ref _exception));
}
