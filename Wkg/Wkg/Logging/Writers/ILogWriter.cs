using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

public interface ILogWriter
{
    void Write(string logEntry, ILogSink sink, LogLevel logLevel);
}

public static class LogWriter
{
    public static ILogWriter Blocking { get; } = new BlockingLogWriter();

    public static ILogWriter Background { get; } = new BackgroundLogWriter();
}