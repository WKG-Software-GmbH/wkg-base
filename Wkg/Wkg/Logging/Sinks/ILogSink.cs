namespace Wkg.Logging.Sinks;

/// <summary>
/// Defines a sink for logging messages.
/// </summary>
public interface ILogSink
{
    /// <summary>
    /// Logs an event.
    /// </summary>
    /// <param name="logEntry">The message to be logged.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> of the message.</param>
    void Log(string logEntry, LogLevel logLevel);
}
