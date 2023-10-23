namespace Wkg.Logging.Sinks;

/// <summary>
/// A thread-safe <see cref="ILogSink"/> that writes log entries to multiple child <see cref="ILogSink"/>s.
/// </summary>
public class ConcurrentSinkCollection : ILogSink
{
    /// <summary>
    /// Gets the child <see cref="ILogSink"/>s.
    /// </summary>
    public IReadOnlyCollection<ILogSink> Sinks { get; }

    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrentSinkCollection"/> class.
    /// </summary>
    /// <param name="sinks">The child <see cref="ILogSink"/>s.</param>
    public ConcurrentSinkCollection(IReadOnlyCollection<ILogSink> sinks) =>
        Sinks = sinks;

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
