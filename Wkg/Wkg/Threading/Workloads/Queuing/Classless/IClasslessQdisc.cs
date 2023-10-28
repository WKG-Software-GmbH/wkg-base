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

public interface IClasslessQdisc<THandle, TQdisc> : IClasslessQdisc<THandle>
    where THandle : unmanaged
    where TQdisc : class, IClasslessQdisc<THandle, TQdisc>
{
    /// <summary>
    /// Creates a new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">The handle uniquely identifying this qdisc. The handle must not be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c> and must not be used by any other qdisc.</param>
    /// <returns>A new <typeparamref name="TQdisc"/> instance with the specified <paramref name="handle"/>.</returns>
    static abstract TQdisc Create(THandle handle);

    /// <summary>
    /// Creates a new anonymous <typeparamref name="TQdisc"/> instance. The handle is not used for classification and may be <c><see langword="default"/>(<typeparamref name="THandle"/>)</c>.
    /// </summary>
    /// <returns>A new anonymous <typeparamref name="TQdisc"/> instance.</returns>
    static abstract TQdisc CreateAnonymous();
}