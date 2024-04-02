using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// An <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> asynchronously on a background thread.
/// </summary>
public class BackgroundLogWriter : ILogWriter
{
    /// <inheritdoc/>
    public void Write(ref readonly LogEntry logEntry, ILogSink sink)
    {
        LogEntryBox box = new(sink, in logEntry);
        ThreadPool.QueueUserWorkItem(LogEntryBox.WriteToSink, box);
    }
}
