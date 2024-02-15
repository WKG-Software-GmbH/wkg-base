namespace Wkg.Threading.Workloads.Exceptions;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public partial class WorkloadSchedulingException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadSchedulingException"/> class.
    /// </summary>
    public WorkloadSchedulingException() => Pass();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadSchedulingException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WorkloadSchedulingException(string? message) : base(message) => Pass();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadSchedulingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    public WorkloadSchedulingException(string? message, Exception? innerException) : base(message, innerException) => Pass();
}
