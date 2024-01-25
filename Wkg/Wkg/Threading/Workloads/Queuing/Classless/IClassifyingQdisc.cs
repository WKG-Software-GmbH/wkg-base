using Wkg.Threading.Workloads.Queuing.Routing;

namespace Wkg.Threading.Workloads.Queuing.Classless;

public interface IClassifyingQdisc : IQdisc
{
    internal INotifyWorkScheduled ParentScheduler { get; }

    internal bool IsCompleted { get; }

    /// <summary>
    /// Enqueues the workload to be executed onto this qdisc.
    /// </summary>
    /// <param name="workload">The workload to be enqueued.</param>
    // TODO: binding can fail if the workload is already completed!
    // we currently don't check for this, which could cause state corruption with emptiness tracking and other things
    internal void Enqueue(AbstractWorkloadBase workload);

    /// <summary>
    /// Checks if the qdisc or any of its children can classify the given state.
    /// </summary>
    /// <param name="state">The state to check.</param>
    /// <returns><see langword="true"/> if the qdisc or any of its children can classify the given state, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    /// A qdisc can classify a state if it can classify the state itself or if any of its children can classify the state.
    /// </remarks>
    internal bool CanClassify(object? state);

    /// <summary>
    /// Attempts to classify the workload and enqueue it to the qdisc or any of its children.
    /// </summary>
    /// <param name="state">The state to classify.</param>
    /// <param name="workload">The workload to enqueue.</param>
    /// <returns><see langword="true"/> if the workload was enqueued, <see langword="false"/> if the workload was not enqueued.</returns>
    /// <remarks>
    /// Qdiscs should first check their children and only check themselves if none of their children can classify the workload.
    /// This allows child qdiscs to override the classification of their parents and allows children to be more restrictive than their parents.
    /// </remarks>
    internal bool TryEnqueue(object? state, AbstractWorkloadBase workload);

    /// <summary>
    /// Attempts to classify the workload and enqueue it to the qdisc itself.
    /// </summary>
    /// <param name="state">The state to classify.</param>
    /// <param name="workload">The workload to enqueue.</param>
    /// <returns><see langword="true"/> if the workload was enqueued, <see langword="false"/> if the workload was not enqueued.</returns>
    internal bool TryEnqueueDirect(object? state, AbstractWorkloadBase workload);

    internal void AssertNotCompleted();
}

public interface IClassifyingQdisc<THandle> : IClassifyingQdisc, IQdisc<THandle> 
    where THandle : unmanaged
{
    /// <summary>
    /// Attempts to enqueue the workload to any direct or indirect child of the qdisc matching the given <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">The handle of the qdisc to enqueue to.</param>
    /// <param name="workload">The workload to enqueue.</param>
    /// <returns><see langword="true"/> if the workload was enqueued, <see langword="false"/> if the workload was not enqueued.</returns>
    /// <remarks>
    /// Qdiscs should only check their children, not their own handle. Checking the qdisc's own handle is the responsibility of the caller. This optimization avoids unnecessary recursion.<br/>
    /// If the qdisc has no children, it should return <see langword="false"/>.
    /// </remarks>
    internal bool TryEnqueueByHandle(THandle handle, AbstractWorkloadBase workload);

    /// <summary>
    /// Called before the <paramref name="workload"/> is enqueued to any direct or indirect child of a qdisc using the routing path represented by <paramref name="routingPathNode"/>.
    /// </summary>
    /// <param name="routingPathNode">The routing path node part of the routing path to the child that will be enqueued to.</param>
    /// <param name="workload">The workload to enqueue.</param>
    /// <remarks>
    /// Qdiscs may override this method to perform additional operations, such as emptiness state updates, before the workload is enqueued to the qdisc or any of its children.
    /// </remarks>
    internal void WillEnqueueFromRoutingPath(ref readonly RoutingPathNode<THandle> routingPathNode, AbstractWorkloadBase workload);

    /// <summary>
    /// Attempts to find the route to the child with the given handle.
    /// </summary>
    /// <param name="handle">The handle of the child to find.</param>
    /// <param name="path">The path to the child with the given handle, if found.</param>
    /// <returns><see langword="true"/> if the child was found, <see langword="false"/> if the child was not found.</returns>
    /// <remarks>
    /// Qdiscs should only check their children, not their own handle. Checking the qdisc's own handle is the responsibility of the caller. This optimization avoids unnecessary recursion.<br/>
    /// If the qdisc has no children, it should return <see langword="false"/>.<br/>
    /// If a direct or indirect child of the qdisc has the given handle, the qdisc should add itself to the path and the method should return <see langword="true"/>.<br/>
    /// If a direct child of the qdisc has the given handle, the path should be completed with that direct child and the method should return <see langword="true"/>.
    /// </remarks>
    internal bool TryFindRoute(THandle handle, ref RoutingPath<THandle> path);

    /// <summary>
    /// Checks if the qdisc contains the child with the given handle.
    /// </summary>
    /// <param name="handle">The handle of the child to find.</param>
    /// <returns><see langword="true"/> if the child was found, <see langword="false"/> if the child was not found.</returns>
    /// <remarks>
    /// Qdiscs should only check their children, not their own handle. Checking the qdisc's own handle is the responsibility of the caller. This optimization avoids unnecessary recursion.<br/>
    /// If the qdisc has no children, it should return <see langword="false"/>.<br/>
    /// 
    /// </remarks>
    internal bool ContainsChild(THandle handle);
}