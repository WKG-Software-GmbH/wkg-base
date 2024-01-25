using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// A <see cref="FifoBackgroundLogWriter"/> that prevents spamming the log sink by suppressing repeating log entries.
/// </summary>
public class FifoBackgroundLogWriterWithSpamControl : FifoBackgroundLogWriter
{
    private readonly object _lock = new();
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
    public override void Write(ref LogEntry logEntry, ILogSink sink)
    {
        lock (_lock)
        {
            if (logEntry.LogMessage.Length > _timeStampSliceOffset && logEntry.LogMessage[_timeStampSliceOffset..].Equals(_lastLogEntry))
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
                    base.Write(ref newEntry, sink);
                    _spamCount = 1;
                }
                _lastLogEntry = logEntry.LogMessage[_timeStampSliceOffset..];
                _lastLogLevel = logEntry.LogLevel;
                base.Write(ref logEntry, sink);
            }
        }
    }
}

