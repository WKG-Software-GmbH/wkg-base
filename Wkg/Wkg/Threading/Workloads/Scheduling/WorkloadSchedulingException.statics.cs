using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

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

    [DoesNotReturn]
    [StackTraceHidden]
    private static void Throw(string message) =>
        throw new WorkloadSchedulingException(message);
}
