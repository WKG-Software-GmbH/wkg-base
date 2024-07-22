using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

internal class LogEntryBox(ILogSink sink, ref readonly LogEntry entry)
{
    public readonly ILogSink Sink = sink;
    public LogEntry Entry = entry;

    public static void WriteToSink(object? state)
    {
        LogEntryBox box = (LogEntryBox)state!;
        box.Sink.Log(ref box.Entry);
    }

    public void WriteToSinkUnsafe() => Sink.LogUnsafe(ref Entry);
}