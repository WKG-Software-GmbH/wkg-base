using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;

namespace Wkg.Threading.Workloads;

/// <summary>
/// Provides an awaiter for awaiting the completion of a <see cref="Workload"/>.
/// </summary>
public readonly struct WorkloadAwaiter : ICriticalNotifyCompletion, INotifyCompletion
{
    private readonly Workload _workload;

    internal WorkloadAwaiter(Workload workload) => _workload = workload;

    /// <summary>
    /// Indicates whether the workload has completed.
    /// </summary>
    /// <remarks>
    /// <see langword="WARNING"/>: Do not change the signature of this property. It is used by compiler generated code.
    /// </remarks>
    public readonly bool IsCompleted => _workload.IsCompleted && _workload.HasResult;

    /// <inheritdoc/>
    public void OnCompleted(Action continuation)
    {
        DebugLog.WriteDiagnostic($"awaiting workload: {_workload}. installing continuation for await", LogWriter.Blocking);
        _workload.SetContinuationForAwait(continuation);
    }

    /// <inheritdoc/>
    public void UnsafeOnCompleted(Action continuation)
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
    public WorkloadResult GetResult()
    {
        DebugLog.WriteDiagnostic($"GetResult invoked for workload: {_workload}", LogWriter.Blocking);
        ValidateEnd(_workload);
        return _workload.GetResultUnsafe();
    }

    internal static void ValidateEnd(AwaitableWorkload workload)
    {
        if (!workload.HasResult)
        {
            DebugLog.WriteDiagnostic($"workload: {workload} is not completed. blocking until completion", LogWriter.Blocking);
            workload.InternalWait(Timeout.Infinite, CancellationToken.None);
        }
        Debug.Assert(workload.IsCompleted, "Workload must be completed at this point.");
        Debug.Assert(workload.HasResult, "Workload must have a result at this point.");
    }
}
