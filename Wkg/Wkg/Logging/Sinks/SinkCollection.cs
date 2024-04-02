using System.Collections.Immutable;

namespace Wkg.Logging.Sinks;

/// <summary>
/// A composite <see cref="ILogSink"/> that logs to multiple child <see cref="ILogSink"/>s.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SinkCollection"/> class.
/// </remarks>
/// <param name="sinks">The child <see cref="ILogSink"/>s.</param>
public class SinkCollection(ImmutableArray<ILogSink> sinks) : ILogSink
{
    /// <summary>
    /// Gets the child <see cref="ILogSink"/>s.
    /// </summary>
    public ImmutableArray<ILogSink> Sinks { get; } = sinks;

    /// <inheritdoc/>
    public void Log(ref readonly LogEntry logEntry) => LogUnsafe(in logEntry);

    /// <inheritdoc/>
    public void LogUnsafe(ref readonly LogEntry logEntry)
    {
        foreach (ILogSink sink in Sinks)
        {
            sink.Log(in logEntry);
        }
    }
}
