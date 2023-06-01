using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// An <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> asynchronously on a background thread.
/// </summary>
public class BackgroundLogWriter : ILogWriter
{
    /// <inheritdoc/>
    public void Write(string logEntry, ILogSink sink, LogLevel logLevel) =>
        ThreadPool.QueueUserWorkItem(_ => sink.Log(logEntry, logLevel));
}
