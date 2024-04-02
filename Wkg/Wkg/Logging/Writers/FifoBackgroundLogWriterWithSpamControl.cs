using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// A <see cref="FifoBackgroundLogWriter"/> that prevents spamming the log sink by suppressing repeating log entries.
/// </summary>
public class FifoBackgroundLogWriterWithSpamControl : FifoBackgroundLogWriter
{
    private readonly int _timeStampSliceOffset;
    private readonly string _spamControlHeader;
    private string _lastLogEntry = string.Empty;
    private LogLevel _lastLogLevel = default;
    private int _spamCount = 1;

    /// <summary>
    /// Creates a new <see cref="FifoBackgroundLogWriterWithSpamControl"/> with the specified <paramref name="timeStampSliceOffset"/>.
    /// </summary>
    /// <param name="timeStampSliceOffset">The offset in the log entry that should be ignored when checking for spam.</param>
    public FifoBackgroundLogWriterWithSpamControl(int timeStampSliceOffset)
    {
        _timeStampSliceOffset = timeStampSliceOffset;
        const string CENTER_MESSAGE = "SPAM CONTROL";
        int paddingRight = (timeStampSliceOffset - CENTER_MESSAGE.Length) / 2 - 1;
        int paddingLeft = timeStampSliceOffset - CENTER_MESSAGE.Length - paddingRight - 2;
        _spamControlHeader = $"[{new('-', paddingLeft)}{CENTER_MESSAGE}{new string('-', paddingRight)}]";
    }

    /// <inheritdoc/>
    public override void Write(ref readonly LogEntry logEntry, ILogSink sink)
    {
        LogEntryBox box = new(sink, in logEntry);
        _loggingQueue.Schedule(box, LogIfAppropriate);
    }

    private void LogIfAppropriate(LogEntryBox box)
    {
        if (box._entry.LogMessage.Length > _timeStampSliceOffset && _lastLogEntry.Length > _timeStampSliceOffset 
            && box._entry.LogMessage.AsSpan()[_timeStampSliceOffset..].SequenceEqual(_lastLogEntry.AsSpan()[_timeStampSliceOffset..]))
        {
            _spamCount++;
            return;
        }
        else
        {
            if (_spamCount > 1)
            {
                LogEntry newEntry = default(LogEntry) with
                {
                    LogMessage = $"{_spamControlHeader} {_spamCount} identical log entries suppressed ...",
                    LogLevel = _lastLogLevel
                };
                box._sink.LogUnsafe(in newEntry);
                _spamCount = 1;
            }
            _lastLogEntry = box._entry.LogMessage;
            _lastLogLevel = box._entry.LogLevel;
            box._sink.LogUnsafe(in box._entry);
        }
    }
}

