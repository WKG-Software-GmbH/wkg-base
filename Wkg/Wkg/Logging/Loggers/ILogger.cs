using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Loggers;

/// <summary>
/// Represents a logger that can be used to log messages and events. A logger can be configured to log to one or more <see cref="ILogSink"/>s and there can be multiple loggers used in parallel. One of these loggers may be used by an implementation of <see cref="ILog"/> to act as a global entry point for logging messages and events.
/// </summary>
public interface ILogger
{
    LogLevel MinimumLogLevel { get; }

    /// <summary>
    /// Writes the provided <paramref name="message"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(string message, LogLevel logLevel = LogLevel.Debug);

    /// <summary>
    /// Writes the provided <paramref name="message"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(string message, ILogWriter logWriter, LogLevel logLevel = LogLevel.Debug);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(Exception exception, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="additionalInfo"/> and <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">Additional information to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="additionalInfo"/> and <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">Additional information to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void Log(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Logs an event with the specified <paramref name="eventName"/> and any additional information to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="instanceName">The name of the instance that raised the event.</param>
    /// <param name="eventName">The name of the event to log.</param>
    /// <param name="eventArgs">The event arguments to log.</param>
    /// <param name="assemblyName">The name of the assembly that contains the type that raised the event.</param>
    /// <param name="className">The name of the type that raised the event.</param>
    void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string? assemblyName = null, string? className = null);

    /// <summary>
    /// Logs an event with the specified <paramref name="eventName"/> and any additional information to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="instanceName">The name of the instance that raised the event.</param>
    /// <param name="eventName">The name of the event to log.</param>
    /// <param name="eventArgs">The event arguments to log.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="assemblyName">The name of the assembly that contains the type that raised the event.</param>
    /// <param name="className">The name of the type that raised the event.</param>
    void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string? assemblyName = null, string? className = null);
}
