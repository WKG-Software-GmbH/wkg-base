namespace Wkg.Threading.Workloads;

public readonly struct WorkloadResult
{
    public readonly Exception? Exception { get; }

    public readonly WorkloadStatus CompletionStatus { get; }

    private WorkloadResult(Exception? exception, WorkloadStatus completionStatus)
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

    public readonly bool IsSuccess => CompletionStatus == WorkloadStatus.RanToCompletion;

    public readonly bool IsFaulted => CompletionStatus == WorkloadStatus.Faulted;

    public readonly bool IsCanceled => CompletionStatus == WorkloadStatus.Canceled;
}
