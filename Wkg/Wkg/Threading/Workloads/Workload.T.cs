using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public class Workload<TResult> : CancelableWorkload
{
    private readonly Func<CancellationFlag, TResult> _func;
    private ValueTask<WorkloadResult<TResult>>? _task;

    internal Workload(Func<CancellationFlag, TResult> func) : this(WorkloadStatus.Created, func) => Pass();

    internal Workload(WorkloadStatus status, Func<CancellationFlag, TResult> func) : base(status)
    {
        _func = func;
    }

    private protected override bool IsResultSet => _task.HasValue;

    public WorkloadResult<TResult> GetResult()
    {
        // TODO: via awaiter / awaitable
        return default;
    }

    private protected override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus)
    {
        // execute the workload
        TResult result = _func(new CancellationFlag(this));

        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            _task = new ValueTask<WorkloadResult<TResult>>(WorkloadResult.CreateCompleted(result));
            DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
            return true;
        }
        else if (preTerminationStatus == WorkloadStatus.Canceled)
        {
            _task = new ValueTask<WorkloadResult<TResult>>(WorkloadResult.CreateCanceled(result));
            DebugLog.WriteDiagnostic($"{this}: Execution was canceled.", LogWriter.Blocking);
            return true;
        }
        return false;
    }

    private protected override void SetCanceledResultUnsafe() => 
        _task = new ValueTask<WorkloadResult<TResult>>(WorkloadResult.CreateCanceled<TResult>());

    private protected override void SetFaultedResultUnsafe(Exception ex) => 
        _task = new ValueTask<WorkloadResult<TResult>>(WorkloadResult.CreateFaulted<TResult>(ex));
}
