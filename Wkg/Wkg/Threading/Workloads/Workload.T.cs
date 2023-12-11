using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public abstract class Workload<TResult> : AwaitableWorkload, IWorkload<TResult>
{
    private volatile object? _result;

    private protected Workload(WorkloadStatus status, WorkloadContextOptions options, CancellationToken cancellationToken)
        : base(status, options, cancellationToken) => Pass();

    private protected abstract TResult ExecuteCore();

    private protected override bool TryExecuteUnsafeCore(out WorkloadStatus preTerminationStatus)
    {
        // execute the workload
        TResult result = ExecuteCore();

        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
        if (preTerminationStatus.IsOneOf(CommonFlags.WillCompleteSuccessfully))
        {
            Volatile.Write(ref _exception, null);
            if (typeof(TResult).IsValueType)
            {
                _result = new WorkloadResultBox<TResult>(result);
            }
            else
            {
                _result = result;
            }
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

    public WorkloadResult<TResult> Result
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
    public void ContinueWith(Action<WorkloadResult<TResult>> continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        if (IsCompleted)
        {
            continuation(GetResultUnsafe());
        }
        else
        {
            DebugLog.WriteDiagnostic($"{this}: Installing continuation for workload.", LogWriter.Blocking);
            ContinueWithCore(new WorkloadContinueWithContination<Workload<TResult>, TResult>(continuation));
        }
    }

    /// <summary>
    /// Gets an awaiter used to await this workload.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not modify or remove this method. It is used by compiler generated code.
    /// </remarks>
    public WorkloadAwaiter<Workload<TResult>, TResult> GetAwaiter() => new(this);

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

    internal WorkloadResult<TResult> GetResultUnsafe()
    {
        object? box = _result;
        TResult? result;
        if (box is null)
        {
            DebugLog.WriteWarning($"{this}: Accessing workload result before it was set.", LogWriter.Blocking);
            result = default;
        }
        else
        {
            // this should get optimized away by the JIT
            if (typeof(TResult).IsValueType)
            {
                result = ReinterpretCast<object, WorkloadResultBox<TResult>>(box).Result;
            }
            else
            {
                result = Unsafe.As<object, TResult>(ref box);
            }
        }
        return new(Status, Volatile.Read(ref _exception), result);
    }

    WorkloadResult<TResult> IWorkload<TResult>.GetResultUnsafe() => GetResultUnsafe();
}
