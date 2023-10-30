namespace Wkg.Threading.Workloads;

public record WorkloadContextOptions
{
    private bool _useSynchronizationContext = true;
    private bool _continueOnCapturedContext = true;

    /// <summary>
    /// Gets or sets a value that indicates whether to marshal the continuation back to the original synchronization context captured when the workload was created.
    /// </summary>
    public bool ContinueOnCapturedContext 
    { 
        get => Volatile.Read(ref _continueOnCapturedContext);
        init => Volatile.Write(ref _continueOnCapturedContext, value); 
    }

    /// <summary>
    /// Gets or sets a value that indicates whether to flow the execution context across async continuations.
    /// </summary>
    public bool FlowExecutionContext
    {
        get => Volatile.Read(ref _useSynchronizationContext);
        init => Volatile.Write(ref _useSynchronizationContext, value);
    }
}
