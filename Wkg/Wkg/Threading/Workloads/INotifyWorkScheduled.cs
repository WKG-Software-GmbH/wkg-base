namespace Wkg.Threading.Workloads;

public interface INotifyWorkScheduled
{
    /// <summary>
    /// Notifies the nearest ancestor workload scheduler that there is work to be done.
    /// </summary>
    internal void OnWorkScheduled();
}
