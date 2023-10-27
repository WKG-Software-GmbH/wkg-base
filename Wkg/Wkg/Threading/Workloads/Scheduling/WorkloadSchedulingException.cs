using System.Diagnostics;
using System.Runtime.Serialization;

namespace Wkg.Threading.Workloads.Scheduling;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public class WorkloadSchedulingException : InvalidOperationException
{
    private readonly string? _overridenStackTrace;

    public WorkloadSchedulingException() => Pass();

    public WorkloadSchedulingException(string? message) : base(message) => Pass();

    public WorkloadSchedulingException(string? message, Exception? innerException) : base(message, innerException) => Pass();

    private WorkloadSchedulingException(string message, string stackTrace, Exception? innerException) : base(message, innerException)
    {
        _overridenStackTrace = stackTrace;
    }

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
        StackTrace stackTrace = new(true);
        WorkloadSchedulingException exception = new(message, stackTrace.ToString(), innerException);
        return exception;
    }

    /// <inheritdoc/>
    public override string? StackTrace => _overridenStackTrace ?? base.StackTrace;
}
