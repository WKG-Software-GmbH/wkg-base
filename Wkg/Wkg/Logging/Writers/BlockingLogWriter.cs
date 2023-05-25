using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

public class BlockingLogWriter : ILogWriter
{
    public void Write(string logEntry, ILogSink sink, LogLevel logLevel) =>
        sink.Log(logEntry, logLevel);
}
