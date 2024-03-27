using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// An <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> on the current thread.
/// </summary>
public class BlockingLogWriter : ILogWriter
{
    /// <inheritdoc/>
    public void Write(ref readonly LogEntry logEntry, ILogSink sink) => 
        sink.Log(in logEntry);
}
