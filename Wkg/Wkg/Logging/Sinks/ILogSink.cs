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
    void Log(ref LogEntry logEntry);
}
