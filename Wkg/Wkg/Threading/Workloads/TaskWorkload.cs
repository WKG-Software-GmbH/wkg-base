using System.Diagnostics;
using System.Threading;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public abstract class TaskWorkload : AsyncWorkload, IWorkload
{
    internal TaskWorkload(WorkloadStatus status, WorkloadContextOptions continuationOptions, CancellationToken cancellationToken) 
        : base(status, continuationOptions, cancellationToken) => Pass();

    private protected abstract Task ExecuteCoreAsync();

    private protected override async Task<WorkloadStatus> TryExecuteUnsafeCoreAsync()
    {
        // execute the workload
        await ExecuteCoreAsync();
        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        WorkloadStatus preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            Volatile.Write(ref _exception, null);
            DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
            return WorkloadStatus.AsyncSuccess;
        }
        else if (preTerminationStatus == WorkloadStatus.Canceled)
        {
            SetCanceledResultUnsafe();
            DebugLog.WriteDiagnostic($"{this}: Execution was canceled.", LogWriter.Blocking);
            return WorkloadStatus.AsyncSuccess;
        }
        return preTerminationStatus;
    }

    public WorkloadResult Result
    {
        get
        {
            if (!IsCompleted)
            {
                InternalWait(Timeout.Infinite, CancellationToken.None);
            }
            return GetResultUnsafe();
        }
    }

    /// <summary>
    /// Creates a continuation that is passed the workload result when the workload completes.
    /// </summary>
    /// <param name="continuation">The action to invoke when the workload completes.</param>
    public void ContinueWith(Action<WorkloadResult> continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        if (IsCompleted)
        {
            continuation(GetResultUnsafe());
        }
        else
        {
            ContinueWithCore(new WorkloadContinueWithContination<TaskWorkload>(continuation));
        }
    }

    /// <summary>
    /// Gets an awaiter used to await this workload.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not modify or remove this method. It is used by compiler generated code.
    /// </remarks>
    public WorkloadAwaiter<TaskWorkload> GetAwaiter() => new(this);

    private protected override void SetCanceledResultUnsafe() =>
        Volatile.Write(ref _exception, null);

    private protected override void SetFaultedResultUnsafe(Exception ex) =>
        Volatile.Write(ref _exception, ex);

    internal WorkloadResult GetResultUnsafe() => new(Status, Volatile.Read(ref _exception));

    WorkloadResult IWorkload.GetResultUnsafe() => GetResultUnsafe();
}
