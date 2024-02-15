namespace Wkg.Threading.Workloads;

public interface INotifyWorkScheduled
{
    /// <summary>
    /// Notifies the nearest ancestor workload scheduler that there is work to be done.
    /// </summary>
    internal void OnWorkScheduled();

    /// <summary>
    /// Disposes the root workload scheduler and waits for all workers to exit.
    /// </summary>
    internal void DisposeRoot();
}
