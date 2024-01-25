using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading.Workloads;

public readonly struct WorkloadResult
{
    public readonly WorkloadStatus CompletionStatus { get; }

    public readonly Exception? Exception { get; }

    internal WorkloadResult(WorkloadStatus completionStatus, Exception? exception)
    {
        Exception = exception;
        CompletionStatus = completionStatus;
    }

    public readonly bool IsSuccess => CompletionStatus.IsOneOf(WorkloadStatus.RanToCompletion);

    [MemberNotNullWhen(true, nameof(Exception))]
    public readonly bool IsFaulted => CompletionStatus.IsOneOf(WorkloadStatus.Faulted);

    public readonly bool IsCanceled => CompletionStatus.IsOneOf(WorkloadStatus.Canceled);

    public readonly void ThrowOnNonSuccess()
    {
        if (IsFaulted)
        {
            throw Exception;
        }
        else if (IsCanceled)
        {
            throw new OperationCanceledException();
        }
    }

    public override string ToString() => CompletionStatus.ToString();
}