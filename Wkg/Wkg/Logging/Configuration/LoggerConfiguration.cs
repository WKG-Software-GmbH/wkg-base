using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Configuration;

public partial class LoggerConfiguration
{
    private readonly List<ILogSink> _logSinks = [];
    private int _mainThreadId = 0;
    private LogLevel _minimumLogLevel = LogLevel.Diagnostic;
    private LogEntryGeneratorFactory _generatorFactory = SimpleLogEntryGenerator.Create;
    private ILogWriter _defaultWriter = LogWriter.Blocking;

    private LoggerConfiguration() => Pass();

    internal CompiledLoggerConfiguration Compile() => new(_minimumLogLevel, new SinkCollection([.. _logSinks]), _mainThreadId, _defaultWriter, _generatorFactory);

    public static partial LoggerConfiguration Create() => new();

    public partial LoggerConfiguration AddSink(ILogSink sink)
    {
        _logSinks.Add(sink);
        return this;
    }

    public partial LoggerConfiguration AddSink<T>() where T : ILogSink, new() =>
        AddSink(new T());

    public partial LoggerConfiguration SetMinimumLogLevel(LogLevel logLevel)
    {
        if (logLevel is >= LogLevel.System)
        {
            throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "The minimum log level must be less than System.");
        }
        _minimumLogLevel = logLevel;
        return this;
    }

    public partial LoggerConfiguration UseDefaultLogWriter(ILogWriter logWriter)
    {
        _defaultWriter = logWriter;
        return this;
    }

    public partial LoggerConfiguration UseEntryGenerator(LogEntryGeneratorFactory generatorFactory)
    {
        _generatorFactory = generatorFactory;
        return this;
    }

    public partial LogFileConfigurationBuilder UseLogFile(string logFileName) =>
        new(logFileName, this);

    public partial LoggerConfiguration RegisterMainThread(Thread thread)
    {
        _mainThreadId = thread.ManagedThreadId;
        return this;
    }
}

/// <summary>
/// A factory for creating log entry generators
/// </summary>
/// <param name="config">The <see cref="CompiledLoggerConfiguration"/> used to create the log entry generator</param>
/// <returns>A new instance of a log entry generator</returns>
public delegate ILogEntryGenerator LogEntryGeneratorFactory(CompiledLoggerConfiguration config);