using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Wkg.Threading.Workloads.Queuing.Classless;

namespace Wkg.Threading.Workloads.Scheduling;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public partial class WorkloadSchedulingException
{
    /// <summary>
    /// Creates a new <see cref="WorkloadSchedulingException"/> with valid stack trace information without the need to throw it. 
    /// Emulates the behavior of throwing and catching an exception.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A new <see cref="WorkloadSchedulingException"/> with valid stack trace information.</returns>
    [StackTraceHidden]
    internal static WorkloadSchedulingException CreateVirtual(string message, Exception? innerException = null)
    {
        WorkloadSchedulingException exception = new(message, innerException);
        ExceptionDispatchInfo.SetCurrentStackTrace(exception);
        return exception;
    }

    [StackTraceHidden]
    internal static void ThrowIfHandleIsDefault<THandle>(THandle handle) where THandle : unmanaged
    {
        if (handle.Equals(default(THandle)))
        {
            Throw($"A qdisc handle must not be the default value of the underlying type '{typeof(THandle).Name}'. Was: '{handle}'.");
        }
    }

    [StackTraceHidden]
    internal static void ThrowIfRoutingPathLeafIsInvalid<THandle>([NotNull] IClassifyingQdisc<THandle>? leaf, THandle handle) where THandle : unmanaged
    {
        if (leaf is null)
        {
            Throw($"Failed to find a route to the qdisc with handle '{handle}'. The constructed routing path is invalid (leaf is null).");
        }
        if (!leaf.Handle.Equals(handle))
        {
            Throw($"Failed to find a route to the qdisc with handle '{handle}'. The constructed routing path is invalid (leaf handle is '{leaf.Handle}', expected '{handle}').");
        }
    }

    [StackTraceHidden]
    internal static void ThrowIfRoutingPathLeafIsCompleted<THandle>(IClassifyingQdisc<THandle>? currentLeaf) where THandle : unmanaged
    {
        if (currentLeaf is not null)
        {
            Throw($"Failed to find a route to the qdisc. The routing path has already been completed (leaf already set to '{currentLeaf.Handle}').");
        }
    }

    [DoesNotReturn]
    [StackTraceHidden]
    internal static void ThrowNoRouteFound<THandle>(THandle handle) where THandle : unmanaged => Throw($"The workload could not be scheduled: no route to handle {handle} was found.");

    [DoesNotReturn]
    [StackTraceHidden]
    internal static void ThrowClassificationFailed(object? state) => Throw($"The workload could not be scheduled: classification failed. No qdisc could classify the state '{state}'.");

    [DoesNotReturn]
    [StackTraceHidden]
    private static void Throw(string message) =>
        throw new WorkloadSchedulingException(message);
}
