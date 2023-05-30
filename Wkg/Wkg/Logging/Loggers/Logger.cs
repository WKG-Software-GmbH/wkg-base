using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Loggers;

public class Logger : ILogger
{
    private readonly ILogEntryGenerator _logEntryGenerator;
    private readonly ConcurrentSinkCollection _sinks;
    private readonly CompiledLoggerConfiguration _config;

    private Logger(CompiledLoggerConfiguration config)
    {
        _config = config;
        _sinks = config.LoggingSinks;
        _logEntryGenerator = config.GeneratorFactory(config);
    }

    public static ILogger Create(LoggerConfiguration config)
    {
        CompiledLoggerConfiguration compiledConfig = config.Compile();
        return new Logger(compiledConfig);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message, LogLevel logLevel = LogLevel.Debug) =>
        Log(message, _config.DefaultLogWriter, logLevel);

    [StackTraceHidden]
    public void Log(string message, ILogWriter logWriter, LogLevel logLevel = LogLevel.Debug)
    {
        string entry = _logEntryGenerator.Generate("Output", message, logLevel);
        logWriter.Write(entry, _sinks, logLevel);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, LogLevel logLevel = LogLevel.Error) =>
        Log(exception, LogWriter.Blocking, logLevel);

    [StackTraceHidden]
    public void Log(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error)
    {
        string entry = _logEntryGenerator.Generate(exception, null, logLevel);
        logWriter.Write(entry, _sinks, logLevel);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error) =>
        Log(exception, additionalInfo, LogWriter.Blocking, logLevel);

    [StackTraceHidden]
    public void Log(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error)
    {
        string entry = _logEntryGenerator.Generate(exception, additionalInfo, logLevel);
        logWriter.Write(entry, _sinks, logLevel);
    }

    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string? assemblyName = null, string? className = null) =>
        Log(instanceName, eventName, eventArgs, _config.DefaultLogWriter, assemblyName, className);

    [StackTraceHidden]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string? assemblyName = null, string? className = null)
    {
        string entry = _logEntryGenerator.Generate(assemblyName, className, instanceName, eventName, eventArgs);
        logWriter.Write(entry, _sinks, LogLevel.Event);
    }
}
