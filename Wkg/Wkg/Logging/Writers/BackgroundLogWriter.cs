using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// An <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> asynchronously on a background thread.
/// </summary>
public class BackgroundLogWriter : ILogWriter
{
    /// <inheritdoc/>
    public void Write(ref LogEntry logEntry, ILogSink sink)
    {
        LogEntryBox box = new(sink, ref logEntry);
        ThreadPool.QueueUserWorkItem(box.WriteToSink);
    }
}

file class LogEntryBox
{
    private readonly ILogSink _sink;
    private LogEntry _entry;

    public LogEntryBox(ILogSink sink, ref LogEntry entry)
    {
        _sink = sink;
        _entry = entry;
    }

    public void WriteToSink(object? _) => _sink.Log(ref _entry);
}