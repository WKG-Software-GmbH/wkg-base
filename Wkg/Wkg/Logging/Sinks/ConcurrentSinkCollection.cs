namespace Wkg.Logging.Sinks;

/// <summary>
/// A thread-safe <see cref="ILogSink"/> that writes log entries to multiple child <see cref="ILogSink"/>s.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConcurrentSinkCollection"/> class.
/// </remarks>
/// <param name="sinks">The child <see cref="ILogSink"/>s.</param>
public class ConcurrentSinkCollection(IReadOnlyCollection<ILogSink> sinks) : ILogSink
{
    /// <summary>
    /// Gets the child <see cref="ILogSink"/>s.
    /// </summary>
    public IReadOnlyCollection<ILogSink> Sinks { get; } = sinks;

    private readonly object _lock = new();

    /// <inheritdoc/>
    public void Log(ref LogEntry logEntry)
    {
        lock (_lock)
        {
            foreach (ILogSink sink in Sinks)
            {
                sink.Log(ref logEntry);
            }
        }
    }
}
