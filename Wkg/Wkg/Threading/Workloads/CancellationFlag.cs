namespace Wkg.Threading.Workloads;

public readonly struct CancellationFlag
{
    private static readonly Workload _neverCancelledWorkload = Workload.CreateInternalUnsafe(null, WorkloadStatus.Invalid);
    internal readonly Workload _workload;

    internal CancellationFlag(Workload workload)
    {
        _workload = workload;
    }

    /// <inheritdoc cref="CancellationToken.IsCancellationRequested"/>
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

    public static CancellationFlag None => new(_neverCancelledWorkload);

    public bool MarkCanceled() => _workload.InternalTryMarkAborted();
}
