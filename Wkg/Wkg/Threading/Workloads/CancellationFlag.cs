using Wkg.Internals.Diagnostic;
using Wkg.Logging.Writers;
using Wkg.Threading.Workloads.WorkloadTypes;

namespace Wkg.Threading.Workloads;

/// <summary>
/// Represents a cancellation request for a workload.
/// </summary>
public readonly struct CancellationFlag
{
    private static readonly AwaitableWorkload _neverCancelledWorkload = new WorkloadImpl(null!, WorkloadStatus.Invalid, null!, CancellationToken.None);
    internal readonly AwaitableWorkload _workload;

    internal CancellationFlag(AwaitableWorkload workload)
    {
        _workload = workload;
    }

    /// <inheritdoc cref="CancellationToken.IsCancellationRequested"/>
    /// <remarks>
    /// When returning prematurely from a workload, because this flag was set, it is required to call <see cref="MarkCanceled"/> before exiting the workload action.
    /// If the workload exits without calling this method, even though client code terminated due to cancellation request, the workload will be considered as successfully completed.
    /// </remarks>
    public bool IsCancellationRequested =>
        // WorkloadStatus.Canceled is a terminal state, so it should never be set during workload execution
        // we still check for it, to be sure to exit as soon as possible in case of a scheduler bug
        _workload.Status.IsOneOf(WorkloadStatus.CancellationRequested | WorkloadStatus.Canceled);

    /// <inheritdoc cref="CancellationToken.ThrowIfCancellationRequested"/>
    public void ThrowIfCancellationRequested()
    {
        if (IsCancellationRequested)
        {
            MarkCanceled();
            throw new OperationCanceledException();
        }
    }

    /// <summary>
    /// Gets a cancellation flag that will never be canceled.
    /// </summary>
    public static CancellationFlag None => new(_neverCancelledWorkload);

    /// <summary>
    /// Marks this workload as canceled.
    /// </summary>
    /// <remarks>
    /// Unlike with <see cref="ThrowIfCancellationRequested"/>, it is required to call this method before exiting the scheduled workload action in case of cancellation.
    /// If the workload exits without calling this method, even though client code terminated due to cancellation request, the workload will be considered as successfully completed.
    /// </remarks>
    /// <returns><see langword="true"/> if the workload was successfully marked as canceled; otherwise, <see langword="false"/>.</returns>
    public bool MarkCanceled()
    {
        DebugLog.WriteDiagnostic($"{_workload}: Marking workload as canceled.", LogWriter.Blocking);
        return _workload.InternalTryMarkAborted();
    }
}
