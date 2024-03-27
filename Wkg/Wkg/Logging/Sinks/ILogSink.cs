namespace Wkg.Logging.Sinks;

/// <summary>
/// Defines a sink for logging messages.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Logs an event.
    /// </summary>
    /// <param name="logEntry">The entry to be logged.</param>
    void Log(ref readonly LogEntry logEntry);

    /// <summary>
    /// Logs an event with the caller guaranteeing that writes are synchronized.
    /// </summary>
    /// <param name="logEntry">The entry to be logged.</param>
    void LogUnsafe(ref readonly LogEntry logEntry);
}
