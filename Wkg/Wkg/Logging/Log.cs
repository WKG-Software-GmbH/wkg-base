using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Loggers;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging;

public class Log : ILog
{
    public static ILogger CurrentLogger { get; private set; } = Logger.Create(
        LoggerConfiguration.Create()
            .UseEntryGenerator<SimpleLogEntryGenerator>()
            .AddSink<ConsoleSink>());

    public static void UseLogger(ILogger logger)
    {
        CurrentLogger = logger;
        CurrentLogger.Log(new string('=', 60), LogLevel.Info);
        CurrentLogger.Log($"{new string(' ', 20)}Logger initialized!", LogLevel.Info);
        CurrentLogger.Log(new string('=', 60), LogLevel.Info);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Diagnostic);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDiagnostic(string message) => 
        CurrentLogger.Log(message, LogLevel.Diagnostic);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message) => 
        CurrentLogger.Log(message, LogLevel.Debug);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDebug(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Debug);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message) => 
        CurrentLogger.Log(message, LogLevel.Info);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInfo(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Info);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message) => 
        CurrentLogger.Log(message, LogLevel.Warning);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteWarning(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Warning);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message) => 
        CurrentLogger.Log(message, LogLevel.Error);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteError(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Error);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFatal(string message) => 
        CurrentLogger.Log(message, LogLevel.Fatal);

    [StackTraceHidden]
    public static void WriteFatal(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Fatal);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logLevel);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logWriter, logLevel);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message) => 
        CurrentLogger.Log(message, LogLevel.Event);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEvent(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Event);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, additionalInfo, logLevel);

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteException(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, additionalInfo, logWriter, logLevel);
}
