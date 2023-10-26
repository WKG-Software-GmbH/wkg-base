namespace Wkg.Threading.Workloads.Queuing.Classless;

public interface IClasslessQdisc : IQdisc
{
    void Enqueue(Workload workload);
}

public interface IClasslessQdisc<THandle> : IClasslessQdisc, IQdisc<THandle> where THandle : unmanaged
{
}
