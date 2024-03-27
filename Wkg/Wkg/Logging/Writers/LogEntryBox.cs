using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

internal class LogEntryBox(ILogSink sink, ref readonly LogEntry entry)
{
    public readonly ILogSink _sink = sink;
    public LogEntry _entry = entry;

    public static void WriteToSink(object? state)
    {
        LogEntryBox box = (LogEntryBox)state!;
        box._sink.Log(ref box._entry);
    }

    public void WriteToSinkUnsafe() => _sink.LogUnsafe(ref _entry);
}