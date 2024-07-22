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
    internal static IProxyLogger ProxyLogger = Logger.CreateProxy(
        LoggerConfiguration.Create()
            .UseEntryGenerator(AotLogEntryGenerator.Create)
            .AddSink<ConsoleSink>());

    /// <inheritdoc/>
    public static ILogger CurrentLogger => ProxyLogger;

    /// <inheritdoc/>
    public static void UseLogger(IProxyLogger logger)
    {
        ProxyLogger = logger;
        ProxyLogger.Log(new string('=', 60), LogWriter.Blocking, LogLevel.Info);
        ProxyLogger.Log($"{new string(' ', 20)}Logger initialized!", LogWriter.Blocking, LogLevel.Info);
        ProxyLogger.Log(new string('=', 60), LogWriter.Blocking, LogLevel.Info);
    }

    /// <inheritdoc/>
    public static void UseConfiguration(LoggerConfiguration configuration) => 
        UseLogger(Logger.CreateProxy(configuration));

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Diagnostic);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Diagnostic);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Debug);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Debug);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Info);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Info);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Warning);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Warning);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Error);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Error);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Fatal);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Fatal);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSystem(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.System);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSystem(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.System);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(exception, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(exception, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Event);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message, ILogWriter logWriter, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, LogLevel.Event);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(exception, additionalInfo, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) => 
        ProxyLogger.LogInternal(exception, additionalInfo, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);
}
