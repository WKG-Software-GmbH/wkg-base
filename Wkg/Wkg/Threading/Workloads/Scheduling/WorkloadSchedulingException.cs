using System.Runtime.Serialization;

namespace Wkg.Threading.Workloads.Scheduling;

/// <summary>
/// The exception that is thrown when an unexpected state or condition occurs in the workload scheduling system.
/// </summary>
public class WorkloadSchedulingException : InvalidOperationException
{
    public WorkloadSchedulingException() => Pass();

    public WorkloadSchedulingException(string? message) : base(message) => Pass();

    public WorkloadSchedulingException(string? message, Exception? innerException) : base(message, innerException) => Pass();

    protected WorkloadSchedulingException(SerializationInfo info, StreamingContext context) : base(info, context) => Pass();
}
