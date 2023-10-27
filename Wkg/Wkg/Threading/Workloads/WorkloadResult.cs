namespace Wkg.Threading.Workloads;

public readonly struct WorkloadResult
{
    public readonly Exception? Exception { get; }

    public readonly WorkloadStatus CompletionStatus { get; }

    internal WorkloadResult(Exception? exception, WorkloadStatus completionStatus)
    {
        Exception = exception;
        CompletionStatus = completionStatus;
    }

    internal static WorkloadResult CreateFaulted(Exception exception) =>
        new(exception, WorkloadStatus.Faulted);

    internal static WorkloadResult CreateCanceled() =>
        new(null, WorkloadStatus.Canceled);

    internal static WorkloadResult CreateCompleted() =>
        new(null, WorkloadStatus.RanToCompletion);

    internal static WorkloadResult<TResult> CreateFaulted<TResult>(Exception exception) =>
        new(exception, WorkloadStatus.Faulted, default);

    internal static WorkloadResult<TResult> CreateCanceled<TResult>() =>
        new(null, WorkloadStatus.Canceled, default);

    internal static WorkloadResult<TResult> CreateCanceled<TResult>(TResult result) =>
        new(null, WorkloadStatus.Canceled, result);

    internal static WorkloadResult<TResult> CreateCompleted<TResult>(TResult result) =>
        new(null, WorkloadStatus.RanToCompletion, result);

    public readonly bool IsSuccess => CompletionStatus == WorkloadStatus.RanToCompletion;

    public readonly bool IsFaulted => CompletionStatus == WorkloadStatus.Faulted;

    public readonly bool IsCanceled => CompletionStatus == WorkloadStatus.Canceled;
}