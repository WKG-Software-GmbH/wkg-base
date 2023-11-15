namespace Wkg.Threading.Workloads.Scheduling;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public partial class WorkloadSchedulingException : InvalidOperationException
{
    public WorkloadSchedulingException() => Pass();

    public WorkloadSchedulingException(string? message) : base(message) => Pass();

    public WorkloadSchedulingException(string? message, Exception? innerException) : base(message, innerException) => Pass();
}
