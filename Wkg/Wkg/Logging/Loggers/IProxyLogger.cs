using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Loggers;

/// <summary>
/// Represents a logger that allows call site information to be manually passed to the logging methods.
/// </summary>
public interface IProxyLogger : ILogger
{
    /// <summary>
    /// Writes the provided <paramref name="message"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(string message, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Debug);

    /// <summary>
    /// Writes the provided <paramref name="message"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(string message, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Debug);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(Exception exception, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="additionalInfo"/> and <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">Additional information to write.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(Exception exception, string additionalInfo, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(Exception exception, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the specified <paramref name="additionalInfo"/> and <paramref name="logLevel"/> to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">Additional information to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    void LogInternal(Exception exception, string additionalInfo, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Logs an event with the specified <paramref name="eventName"/> and any additional information to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="instanceName">The name of the instance that raised the event.</param>
    /// <param name="eventName">The name of the event to log.</param>
    /// <param name="eventArgs">The event arguments to log.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="assemblyName">The name of the assembly that contains the type that raised the event.</param>
    /// <param name="className">The name of the type that raised the event.</param>
    void LogInternal<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string callerFilePath, string callerMemberName, int callerLineNumber, string? assemblyName = null, string? className = null);

    /// <summary>
    /// Logs an event with the specified <paramref name="eventName"/> and any additional information to the <see cref="ILogSink"/>s configured for this <see cref="ILogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="instanceName">The name of the instance that raised the event.</param>
    /// <param name="eventName">The name of the event to log.</param>
    /// <param name="eventArgs">The event arguments to log.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">The path of the file that contains the member that called the method.</param>
    /// <param name="callerMemberName">The name of the member that called the method.</param>
    /// <param name="callerLineNumber">The line number in the file at which the method was called.</param>
    /// <param name="assemblyName">The name of the assembly that contains the type that raised the event.</param>
    /// <param name="className">The name of the type that raised the event.</param>
    void LogInternal<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, string? assemblyName = null, string? className = null);
}
