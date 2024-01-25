using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging;

/// <summary>
/// A default implementation of <see cref="ILog"/> that uses the default <see cref="Logger"/> implementation to log messages.
/// </summary>
public class Log : ILog
{
    /// <inheritdoc/>
    public static ILogger CurrentLogger { get; private set; } = Logger.Create(
        LoggerConfiguration.Create()
            .UseEntryGenerator<SimpleLogEntryGenerator>()
            .AddSink<ConsoleSink>());

    /// <inheritdoc/>
    public static void UseLogger(ILogger logger)
    {
        CurrentLogger = logger;
        CurrentLogger.Log(new string('=', 60), LogWriter.Blocking, LogLevel.Info);
        CurrentLogger.Log($"{new string(' ', 20)}Logger initialized!", LogWriter.Blocking, LogLevel.Info);
        CurrentLogger.Log(new string('=', 60), LogWriter.Blocking, LogLevel.Info);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Diagnostic);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message) => 
        CurrentLogger.Log(message, LogLevel.Diagnostic);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message) => 
        CurrentLogger.Log(message, LogLevel.Debug);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Debug);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message) => 
        CurrentLogger.Log(message, LogLevel.Info);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Info);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message) => 
        CurrentLogger.Log(message, LogLevel.Warning);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Warning);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message) => 
        CurrentLogger.Log(message, LogLevel.Error);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Error);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message) => 
        CurrentLogger.Log(message, LogLevel.Fatal);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Fatal);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logWriter, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message) => 
        CurrentLogger.Log(message, LogLevel.Event);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Event);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, additionalInfo, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, additionalInfo, logWriter, logLevel);
}
