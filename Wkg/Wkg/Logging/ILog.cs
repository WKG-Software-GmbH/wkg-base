using Wkg.Logging.Loggers;
using Wkg.Logging.Writers;

namespace Wkg.Logging;

/// <summary>
/// Represents the global entry point for logging messages and events to a configured <see cref="ILogger"/>.
/// </summary>
public interface ILog
{
    /// <summary>
    /// Gets the current <see cref="ILogger"/>.
    /// </summary>
    static abstract ILogger CurrentLogger { get; }

    /// <summary>
    /// Updates the globally used <see cref="ILogger"/>.
    /// </summary>
    static abstract void UseLogger(ILogger logger);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Diagnostic"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteDiagnostic(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Diagnostic"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteDiagnostic(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Debug"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteDebug(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Debug"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteDebug(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Info"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteInfo(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Info"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteInfo(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Warning"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteWarning(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Warning"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteWarning(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Error"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteError(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Error"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteError(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Fatal"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteFatal(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Fatal"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteFatal(string message, ILogWriter logWriter);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    static abstract void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the provided <paramref name="additionalInfo"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">The additional info to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    static abstract void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    static abstract void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the provided <paramref name="additionalInfo"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">The additional info to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    static abstract void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as an <see cref="LogLevel.Event"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    static abstract void WriteEvent(string message);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as an <see cref="LogLevel.Event"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    static abstract void WriteEvent(string message, ILogWriter logWriter);
}
