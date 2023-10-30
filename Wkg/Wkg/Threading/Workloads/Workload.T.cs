using System;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public class Workload<TResult> : AwaitableWorkload
{
    private readonly Func<CancellationFlag, TResult> _func;
    private TResult? _result;

    internal Workload(Func<CancellationFlag, TResult> func, WorkloadContextOptions options, CancellationToken cancellationToken)
        : this(func, WorkloadStatus.Created, options, cancellationToken) => Pass();

    internal Workload(Func<CancellationFlag, TResult> func, WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken)
    {
        _func = func;
    }

    public WorkloadAwaiter<TResult> GetAwaiter() => new(this);

    private protected override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus)
    {
        // execute the workload
        TResult result = _func(new CancellationFlag(this));

        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            Volatile.Write(ref _exception, null);
            _result = result;
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

    private protected override void SetCanceledResultUnsafe()
    {
        Volatile.Write(ref _exception, null);
        _result = default;
    }

    private protected override void SetFaultedResultUnsafe(Exception ex)
    {
        Volatile.Write(ref _exception, ex);
        _result = default;
    }

    internal WorkloadResult<TResult> GetResultUnsafe() => new(Status, Volatile.Read(ref _exception), _result);
}
