namespace Wkg.Threading.Workloads.Exceptions;

/// <summary>
/// Represents an exception used to communicate workload cancellation.
/// </summary>
public class WorkloadCanceledException : OperationCanceledException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadCanceledException"/> class.
    /// </summary>
    public WorkloadCanceledException() => Pass();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadCanceledException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WorkloadCanceledException(string? message) : base(message) => Pass();

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkloadCanceledException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a <see langword="null"/> reference if no inner exception is specified.</param>
    public WorkloadCanceledException(string? message, Exception? innerException) : base(message, innerException) => Pass();
}
