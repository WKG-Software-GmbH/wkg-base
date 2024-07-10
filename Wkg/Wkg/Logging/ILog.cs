using System.Runtime.CompilerServices;
using Wkg.Logging.Configuration;
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
    /// Updates the globally used <see cref="IProxyLogger"/>.
    /// </summary>
    static abstract void UseLogger(IProxyLogger logger);

    /// <summary>
    /// Creates a new globally used <see cref="IProxyLogger"/> with the provided <paramref name="configuration"/>.
    /// </summary>
    static abstract void UseConfiguration(LoggerConfiguration configuration);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Diagnostic"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteDiagnostic(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Diagnostic"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteDiagnostic(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Debug"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteDebug(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Debug"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteDebug(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Info"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteInfo(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Info"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteInfo(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Warning"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteWarning(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Warning"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteWarning(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Error"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteError(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Error"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteError(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Fatal"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteFatal(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.Fatal"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteFatal(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.System"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteSystem(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as a <see cref="LogLevel.System"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteSystem(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the provided <paramref name="additionalInfo"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">The additional info to write.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="exception"/> with the provided <paramref name="additionalInfo"/> at the specified <paramref name="logLevel"/> to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to write.</param>
    /// <param name="additionalInfo">The additional info to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as an <see cref="LogLevel.Event"/> entry to the <see cref="CurrentLogger"/> using the configured default <see cref="ILogWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteEvent(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Writes the provided <paramref name="message"/> as an <see cref="LogLevel.Event"/> entry to the <see cref="CurrentLogger"/> using the specified <paramref name="logWriter"/>.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to use.</param>
    /// <param name="callerFilePath">Compiler-substituted value indicating the path of the file that contains the member that called the method. Leave as default.</param>
    /// <param name="callerMemberName">Compiler-substituted value indicating the name of the member that called the method. Leave as default.</param>
    /// <param name="callerLineNumber">Compiler-substituted value indicating the line number in the file at which the method was called. Leave as default.</param>
    static abstract void WriteEvent(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0);
}
