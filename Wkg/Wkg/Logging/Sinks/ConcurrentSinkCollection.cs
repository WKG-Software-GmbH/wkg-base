namespace Wkg.Logging.Sinks;

public class ConcurrentSinkCollection : ILogSink
{
    public IReadOnlyCollection<ILogSink> Sinks { get; }

    private readonly object _lock = new();

    public ConcurrentSinkCollection(IReadOnlyCollection<ILogSink> sinks) =>
        Sinks = sinks;

    public void Log(string logEntry, LogLevel logLevel)
    {
        lock (_lock)
        {
            foreach (ILogSink sink in Sinks)
            {
                sink.Log(logEntry, logLevel);
            }
        }
    }
}
