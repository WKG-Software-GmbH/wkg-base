using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public class Workload : CancelableWorkload
{
    private readonly Action<CancellationFlag> _action;
    private ValueTask<WorkloadResult>? _task;

    internal Workload(Action<CancellationFlag> action) : this(WorkloadStatus.Created, action) => Pass();

    internal Workload(WorkloadStatus status, Action<CancellationFlag> action) : base(status)
    {
        _action = action;
    }

    private protected override bool IsResultSet => _task.HasValue;

    private protected override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus)
    {
        // execute the workload
        _action(new CancellationFlag(this));
        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCompleted());
            DebugLog.WriteDiagnostic($"{this}: Successfully completed execution.", LogWriter.Blocking);
            return true;
        }
        else if (preTerminationStatus == WorkloadStatus.Canceled)
        {
            _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());
            DebugLog.WriteDiagnostic($"{this}: Execution was canceled.", LogWriter.Blocking);
            return true;
        }
        return false;
    }

    public WorkloadResult GetResult()
    {
        // TODO: via awaiter / awaitable
        return default;
    }

    private protected override void SetCanceledResultUnsafe() =>
        _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateCanceled());

    private protected override void SetFaultedResultUnsafe(Exception ex) =>
        _task = new ValueTask<WorkloadResult>(WorkloadResult.CreateFaulted(ex));
}
