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
public class Logger : IProxyLogger
{
    private readonly ILogEntryGenerator _logEntryGenerator;
    private readonly SinkCollection _sinks;
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

    /// <summary>
    /// Creates a new <see cref="IProxyLogger"/> instance using the specified <see cref="LoggerConfiguration"/>.
    /// </summary>
    /// <param name="config">The <see cref="LoggerConfiguration"/> to use.</param>
    public static IProxyLogger CreateProxy(LoggerConfiguration config)
    {
        CompiledLoggerConfiguration compiledConfig = config.Compile();
        return new Logger(compiledConfig);
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message, LogLevel logLevel = LogLevel.Debug, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(message, _config.DefaultLogWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(string message, ILogWriter logWriter, LogLevel logLevel = LogLevel.Debug, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(message, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(exception, null!, LogWriter.Blocking, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(exception, null!, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, string additionalInfo, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(exception, additionalInfo, LogWriter.Blocking, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Exception exception, string additionalInfo, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(exception, additionalInfo, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string? assemblyName = null, string? className = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(instanceName, eventName, eventArgs, _config.DefaultLogWriter, callerFilePath, callerMemberName, callerLineNumber, assemblyName, className);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string? assemblyName = null, string? className = null, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = 0) =>
        LogInternal(instanceName, eventName, eventArgs, logWriter, callerFilePath, callerMemberName, callerLineNumber, assemblyName, className);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInternal(string message, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Debug) =>
        LogInternal(message, _config.DefaultLogWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void LogInternal(string message, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Debug)
    {
        if (logLevel >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = logLevel;
            entry.Type = LogEntryType.Message;
            entry.CallerInfo = new CallerInfo(callerFilePath, callerMemberName, callerLineNumber);
            _logEntryGenerator.Generate(ref entry, "Output", message);
            logWriter.Write(ref entry, _sinks);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInternal(Exception exception, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error) =>
        LogInternal(exception, null!, LogWriter.Blocking, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInternal(Exception exception, string additionalInfo, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error) =>
        LogInternal(exception, additionalInfo, LogWriter.Blocking, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInternal(Exception exception, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error) =>
        LogInternal(exception, null!, logWriter, callerFilePath, callerMemberName, callerLineNumber, logLevel);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void LogInternal(Exception exception, string additionalInfo, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, LogLevel logLevel = LogLevel.Error)
    {
        if (logLevel >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = logLevel;
            entry.Type = LogEntryType.Exception;
            entry.CallerInfo = new CallerInfo(callerFilePath, callerMemberName, callerLineNumber);
            _logEntryGenerator.Generate(ref entry, exception, additionalInfo);
            logWriter.Write(ref entry, _sinks);
        }
    }

    /// <inheritdoc/>
    [StackTraceHidden]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void LogInternal<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string callerFilePath, string callerMemberName, int callerLineNumber, string? assemblyName = null, string? className = null) =>
        LogInternal(instanceName, eventName, eventArgs, _config.DefaultLogWriter, callerFilePath, callerMemberName, callerLineNumber, assemblyName, className);

    /// <inheritdoc/>
    [StackTraceHidden]
    public void LogInternal<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string callerFilePath, string callerMemberName, int callerLineNumber, string? assemblyName = null, string? className = null)
    {
        if (LogLevel.Event >= MinimumLogLevel)
        {
            LogEntry entry = default;
            entry.LogLevel = LogLevel.Event;
            entry.Type = LogEntryType.Event;
            entry.CallerInfo = new CallerInfo(callerFilePath, callerMemberName, callerLineNumber);
            entry.AssemblyName = assemblyName;
            _logEntryGenerator.Generate(ref entry, className, instanceName, eventName, eventArgs);
            logWriter.Write(ref entry, _sinks);
        }
    }
}
