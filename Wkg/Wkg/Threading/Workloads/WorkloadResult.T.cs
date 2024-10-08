﻿using System.Diagnostics.CodeAnalysis;
using System.Text;
using Wkg.Text;
using Wkg.Threading.Workloads.Exceptions;

namespace Wkg.Threading.Workloads;

public readonly struct WorkloadResult<TResult>
{
    public readonly WorkloadStatus CompletionStatus { get; }

    public readonly Exception? Exception { get; }

    public readonly TResult? Result { get; }

    internal WorkloadResult(WorkloadStatus completionStatus, Exception? exception, TResult? result)
    {
        Exception = exception;
        CompletionStatus = completionStatus;
        Result = result;
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
            throw new WorkloadCanceledException();
        }
    }

    public readonly bool TryGetResult([NotNullWhen(true)] out TResult? result)
    {
        result = Result;
        return IsSuccess;
    }

    public override string ToString()
    {
        StringBuilder sb = StringBuilderPool.Shared.Rent(256);

        sb.Append(CompletionStatus.ToString());

        if (IsFaulted)
        {
            sb.Append(" (");
            sb.Append(Exception!.GetType().Name);
            sb.Append(": ");
            sb.Append(Exception.Message);
            sb.Append(')');
        }
        else if (IsSuccess)
        {
            sb.Append(" (")
                .Append(Result?.GetType().Name ?? "<null>")
                .Append(": ")
                .Append(Result?.ToString() ?? "<null>")
                .Append(')');
        }

        string result = sb.ToString();
        StringBuilderPool.Shared.Return(sb);
        return result;
    }
}