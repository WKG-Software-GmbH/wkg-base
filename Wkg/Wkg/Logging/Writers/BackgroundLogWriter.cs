using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

public class BackgroundLogWriter : ILogWriter
{
    public void Write(string logEntry, ILogSink sink, LogLevel logLevel) =>
        ThreadPool.QueueUserWorkItem(_ => sink.Log(logEntry, logLevel));
}
