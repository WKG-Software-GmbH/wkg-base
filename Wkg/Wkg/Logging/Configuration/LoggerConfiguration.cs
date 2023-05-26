using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;

namespace Wkg.Logging.Configuration;

public partial class LoggerConfiguration
{
    private readonly List<ILogSink> _logSinks = new();
    private int _mainThreadId = 0;
    private Func<CompiledLoggerConfiguration, ILogEntryGenerator> _generatorFactory = TracingLogEntryGenerator.Create;

    private LoggerConfiguration()
    {
    }

    internal CompiledLoggerConfiguration Compile() => new(new ConcurrentSinkCollection(_logSinks.ToArray()), _mainThreadId, _generatorFactory);

    public static partial LoggerConfiguration Create() => new();

    public partial LoggerConfiguration AddSink(ILogSink sink)
    {
        _logSinks.Add(sink);
        return this;
    }

    public partial LoggerConfiguration AddSink<T>() where T : ILogSink, new() =>
        AddSink(new T());

    public LoggerConfiguration UseEntryGenerator<TGenerator>() where TGenerator : class, ILogEntryGenerator<TGenerator>
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
