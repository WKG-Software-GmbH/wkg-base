using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Configuration;

public partial class LoggerConfiguration
{
    private readonly List<ILogSink> _logSinks = [];
    private int _mainThreadId = 0;
    private LogLevel _minimumLogLevel = LogLevel.Diagnostic;
    private Func<CompiledLoggerConfiguration, ILogEntryGenerator> _generatorFactory = TracingLogEntryGenerator.Create;
    private ILogWriter _defaultWriter = LogWriter.Blocking;

    private LoggerConfiguration()
    {
    }

    internal CompiledLoggerConfiguration Compile() => new(_minimumLogLevel, new ConcurrentSinkCollection(_logSinks.ToArray()), _mainThreadId, _defaultWriter, _generatorFactory);

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
        _minimumLogLevel = logLevel;
        return this;
    }

    public partial LoggerConfiguration UseDefaultLogWriter(ILogWriter logWriter)
    {
        _defaultWriter = logWriter;
        return this;
    }

    public partial LoggerConfiguration UseEntryGenerator<TGenerator>() where TGenerator : class, ILogEntryGenerator<TGenerator>
    {
        _generatorFactory = TGenerator.Create;
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
