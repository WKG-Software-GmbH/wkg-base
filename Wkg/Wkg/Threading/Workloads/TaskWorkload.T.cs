using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.Continuations;

namespace Wkg.Threading.Workloads;

using CommonFlags = WorkloadStatus.CommonFlags;

public abstract class TaskWorkload<TResult> : AsyncWorkload, IWorkload<TResult>
{
    private volatile object? _result;

    internal TaskWorkload(WorkloadStatus status, WorkloadContextOptions continuationOptions, CancellationToken cancellationToken) 
        : base(status, continuationOptions, cancellationToken) => Pass();

    private protected abstract Task<TResult> ExecuteCoreAsync();

    private protected async override Task<WorkloadStatus> TryExecuteUnsafeCoreAsync()
    {
        // execute the workload
        TResult result = await ExecuteCoreAsync();

        // if cancellation was requested, but the workload didn't honor it,
        // then we'll just ignore it and treat it as a successful completion
        WorkloadStatus preTerminationStatus = Atomic.TestAnyFlagsExchange(ref _status, WorkloadStatus.RanToCompletion, CommonFlags.WillCompleteSuccessfully);
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
            ContinueWithCore(new WorkloadContinueWithContination<TaskWorkload<TResult>, TResult>(continuation));
        }
    }

    /// <summary>
    /// Gets an awaiter used to await this workload.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not modify or remove this method. It is used by compiler generated code.
    /// </remarks>
    public WorkloadAwaiter<TaskWorkload<TResult>, TResult> GetAwaiter() => new(this);

    private protected override void SetCanceledResultUnsafe()
    {
        Volatile.Write(ref _exception, null);
        _result = null;
    }

    private protected override void SetFaultedResultUnsafe(Exception ex)
    {
        Volatile.Write(ref _exception, ex);
        _result = null;
    }

    internal WorkloadResult<TResult> GetResultUnsafe()
    {
        object? resultContainer = _result;
        TResult? result;
        if (resultContainer is null)
        {
            DebugLog.WriteWarning($"{this}: Accessing workload result before it was set.", LogWriter.Blocking);
            result = default;
        }
        else
        {
            if (typeof(TResult).IsValueType)
            {
                result = ReinterpretCast<object, WorkloadResultBox<TResult>>(resultContainer).Result;
            }
            else
            {
                result = Unsafe.As<object,TResult>(ref resultContainer);
            }
        }
        return new(Status, Volatile.Read(ref _exception), result);
    }

    WorkloadResult<TResult> IWorkload<TResult>.GetResultUnsafe() => GetResultUnsafe();
}
