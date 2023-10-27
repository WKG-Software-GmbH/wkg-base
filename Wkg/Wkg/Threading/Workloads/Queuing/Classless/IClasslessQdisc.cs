namespace Wkg.Threading.Workloads.Queuing.Classless;

public interface IClasslessQdisc : IQdisc
{
    void Enqueue(AbstractWorkloadBase workload);
}

public interface IClasslessQdisc<THandle> : IClasslessQdisc, IQdisc<THandle> 
    where THandle : unmanaged
{
}

public interface IClasslessQdisc<THandle, TQdisc> : IClasslessQdisc<THandle>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    static abstract TQdisc Create(THandle handle);
}