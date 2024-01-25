using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

/// <summary>
/// Provides an awaiter for awaiting the completion of a <see cref="Workload{TResult}"/>.
/// </summary>
/// <typeparam name="TWorkload">The type of the workload.</typeparam>
/// <typeparam name="TResult">The type of the result produced by the workload.</typeparam>
public readonly struct WorkloadAwaiter<TWorkload, TResult> : ICriticalNotifyCompletion, INotifyCompletion
    where TWorkload : AwaitableWorkload, IWorkload<TResult>
{
    private readonly TWorkload _workload;

    internal WorkloadAwaiter(TWorkload workload) => _workload = workload;

    /// <summary>
    /// Indicates whether the workload has completed.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not change the signature of this property. It is used by compiler generated code.
    /// </remarks>
    public readonly bool IsCompleted => _workload.IsCompleted;

    /// <inheritdoc/>
    public readonly void OnCompleted(Action continuation)
    {
        DebugLog.WriteDiagnostic($"awaiting workload: {_workload}. installing continuation for await", LogWriter.Blocking);
        _workload.SetContinuationForAwait(continuation);
    }

    /// <inheritdoc/>
    public readonly void UnsafeOnCompleted(Action continuation)
    {
        DebugLog.WriteDiagnostic($"awaiting workload: {_workload}. installing continuation for await", LogWriter.Blocking);
        _workload.SetContinuationForAwait(continuation);
    }

    /// <summary>
    /// Gets the result of the workload. This method blocks until the workload has completed.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not modify or remove this method. It is used by compiler generated code.
    /// </remarks>
    public readonly WorkloadResult<TResult> GetResult()
    {
        DebugLog.WriteDiagnostic($"GetResult invoked for workload: {_workload}", LogWriter.Blocking);
        WorkloadAwaiterCore.ValidateEnd(_workload);
        return _workload.GetResultUnsafe();
    }
}
