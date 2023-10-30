using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Wkg.Threading.Workloads.Scheduling;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public class WorkloadSchedulingException : InvalidOperationException
{
    public WorkloadSchedulingException() => Pass();

    public WorkloadSchedulingException(string? message) : base(message) => Pass();

    public WorkloadSchedulingException(string? message, Exception? innerException) : base(message, innerException) => Pass();

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
}
