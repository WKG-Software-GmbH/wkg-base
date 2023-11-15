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
        ThreadPool.QueueUserWorkItem(LogEntryBox.WriteToSink, box);
    }
}

file class LogEntryBox(ILogSink sink, ref readonly LogEntry entry)
{
    private readonly ILogSink _sink = sink;
    private LogEntry _entry = entry;

    public static void WriteToSink(object? state)
    {
        LogEntryBox box = (LogEntryBox)state!;
        box._sink.Log(ref box._entry);
    }
}