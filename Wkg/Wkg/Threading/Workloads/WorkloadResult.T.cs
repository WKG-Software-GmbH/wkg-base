namespace Wkg.Threading.Workloads;

public readonly struct WorkloadResult<TResult>
{
    public readonly Exception? Exception { get; }

    public readonly WorkloadStatus CompletionStatus { get; }

    public readonly TResult? Result { get; }

    internal WorkloadResult(Exception? exception, WorkloadStatus completionStatus, TResult? result)
    {
        Exception = exception;
        CompletionStatus = completionStatus;
        Result = result;
    }

    public readonly bool IsSuccess => CompletionStatus == WorkloadStatus.RanToCompletion;

    public readonly bool IsFaulted => CompletionStatus == WorkloadStatus.Faulted;

    public readonly bool IsCanceled => CompletionStatus == WorkloadStatus.Canceled;

    public readonly bool TryGetResult(out TResult? result)
    {
        result = Result;
        return IsSuccess;
    }
}