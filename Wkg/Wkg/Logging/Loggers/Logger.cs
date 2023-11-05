using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wkg.Logging.Configuration;
using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Loggers;

/// <summary>
/// A default implementation of <see cref="ILogger"/> that can write log entries to multiple <see cref="ILogSink"/>s.
/// </summary>
public class Logger : ILogger
{
    private readonly ILogEntryGenerator _logEntryGenerator;
    private readonly ConcurrentSinkCollection _sinks;
    private readonly CompiledLoggerConfiguration _config;

    private uint _minimumLevel;

    private Logger(CompiledLoggerConfiguration config)
    {
        _config = config;
        _sinks = config.LoggingSinks;
        _logEntryGenerator = config.GeneratorFactory(config);
        _minimumLevel = (uint)config.MinimumLogLevel;
    }

    /// <inheritdoc/>
    public LogLevel MinimumLogLevel
    {
        get => (LogLevel)Volatile.Read(ref _minimumLevel);
        set => Volatile.Write(ref _minimumLevel, (uint)value);
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance using the specified <see cref="LoggerConfiguration"/>.
    /// </summary>
    /// <param name="config">The <see cref="LoggerConfiguration"/> to use.</param>
    public static ILogger Create(LoggerConfiguration config)
    {
        CompiledLoggerConfiguration compiledConfig = config.Compile();
        return new Logger(compiledConfig);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message, LogLevel logLevel = LogLevel.Debug) =>
        Log(message, _config.DefaultLogWriter, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void Log(string message, ILogWriter logWriter, LogLevel logLevel = LogLevel.Debug)
    {
        if (logLevel >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = logLevel;
            entry.Type = LogEntryType.Message;
            _logEntryGenerator.Generate(ref entry, "Output", message);
            logWriter.Write(ref entry, _sinks);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, LogLevel logLevel = LogLevel.Error) =>
        Log(exception, LogWriter.Blocking, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void Log(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error)
    {
        if (logLevel >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = logLevel;
            entry.Type = LogEntryType.Exception;
            _logEntryGenerator.Generate(ref entry, exception, null);
            logWriter.Write(ref entry, _sinks);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error) =>
        Log(exception, additionalInfo, LogWriter.Blocking, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void Log(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error)
    {
        if (logLevel >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = logLevel;
            entry.Type = LogEntryType.Exception;
            _logEntryGenerator.Generate(ref entry, exception, additionalInfo);
            logWriter.Write(ref entry, _sinks);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string? assemblyName = null, string? className = null) =>
        Log(instanceName, eventName, eventArgs, _config.DefaultLogWriter, assemblyName, className);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string? assemblyName = null, string? className = null)
    {
        if (LogLevel.Event >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = LogLevel.Event;
            entry.Type = LogEntryType.Event;
            _logEntryGenerator.Generate(ref entry, assemblyName, className, instanceName, eventName, eventArgs);
            logWriter.Write(ref entry, _sinks);
        }
    }
}
