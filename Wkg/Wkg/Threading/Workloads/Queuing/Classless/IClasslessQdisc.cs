namespace Wkg.Threading.Workloads.Queuing.Classless;

public interface IClasslessQdisc : IQdisc
{
    /// <summary>
    /// Enqueues the workload to be executed onto this qdisc.
    /// </summary>
    /// <param name="workload">The workload to be enqueued.</param>
    internal void Enqueue(AbstractWorkloadBase workload);
}

public interface IClasslessQdisc<THandle> : IClasslessQdisc, IQdisc<THandle> 
    where THandle : unmanaged
{
}