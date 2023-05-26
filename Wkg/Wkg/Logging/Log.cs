using System.Diagnostics;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
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
    public static void WriteDiagnostic(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Diagnostic);

    [StackTraceHidden]
    public static void WriteDiagnostic(string message) => 
        CurrentLogger.Log(message, LogLevel.Diagnostic);

    [StackTraceHidden]
    public static void WriteDebug(string message) => 
        CurrentLogger.Log(message, LogLevel.Debug);

    [StackTraceHidden]
    public static void WriteDebug(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Debug);

    [StackTraceHidden]
    public static void WriteInfo(string message) => 
        CurrentLogger.Log(message, LogLevel.Info);

    [StackTraceHidden]
    public static void WriteInfo(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Info);

    [StackTraceHidden]
    public static void WriteWarning(string message) => 
        CurrentLogger.Log(message, LogLevel.Warning);

    [StackTraceHidden]
    public static void WriteWarning(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Warning);

    [StackTraceHidden]
    public static void WriteError(string message) => 
        CurrentLogger.Log(message, LogLevel.Error);

    [StackTraceHidden]
    public static void WriteError(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Error);

    [StackTraceHidden]
    public static void WriteFatal(string message) => 
        CurrentLogger.Log(message, LogLevel.Fatal);

    [StackTraceHidden]
    public static void WriteFatal(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Fatal);

    [StackTraceHidden]
    public static void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logLevel);

    [StackTraceHidden]
    public static void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error) => 
        CurrentLogger.Log(exception, logWriter, logLevel);

    [StackTraceHidden]
    public static void WriteEvent(string message) => 
        CurrentLogger.Log(message, LogLevel.Event);

    [StackTraceHidden]
    public static void WriteEvent(string message, ILogWriter logWriter) => 
        CurrentLogger.Log(message, logWriter, LogLevel.Event);
}
