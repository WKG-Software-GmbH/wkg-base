using Wkg.Logging.Sinks;
using Wkg.Threading.Workloads.Configuration;
using Wkg.Threading.Workloads.Factories;
using Wkg.Threading.Workloads.Internal;
using Wkg.Threading.Workloads.Queuing.Classless.Fifo;

namespace Wkg.Logging.Writers;

/// <summary>
/// A <see cref="ILogWriter"/> that writes log entries to a <see cref="ILogSink"/> on a background thread preserving the order of the log entries.
/// </summary>
public class FifoBackgroundLogWriter : ILogWriter
{
    /// <summary>
    /// The queueing discipline used to schedule the log entries.
    /// </summary>
    protected readonly ClasslessWorkloadFactory<SingleQdiscHandle> _loggingQueue = WorkloadFactoryBuilder.Create<SingleQdiscHandle>()
        .UseMaximumConcurrency(1)
        .FlowExecutionContextToContinuations(false)
        .RunContinuationsOnCapturedContext(false)
        .UseAnonymousWorkloadPooling(8)
        .UseClasslessRoot<Fifo>(SingleQdiscHandle.Root);

    /// <inheritdoc/>
    public void Write(ref LogEntry logEntry, ILogSink sink)
    {
        LogEntryBox box = new(sink, ref logEntry);
        _loggingQueue.Schedule(box.WriteToSink);
    }
}
