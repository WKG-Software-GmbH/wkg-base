using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

internal class LogEntryBox(ILogSink sink, ref readonly LogEntry entry)
{
    private readonly ILogSink _sink = sink;
    private LogEntry _entry = entry;

    public static void WriteToSink(object? state)
    {
        LogEntryBox box = (LogEntryBox)state!;
        box._sink.Log(ref box._entry);
    }

    public void WriteToSink() => _sink.Log(ref _entry);
}