namespace Wkg.Logging;

/// <summary>
/// Specifies the type of a log entry.
/// </summary>
public enum LogEntryType
{
    /// <summary>
    /// The log entry is a simple message.
    /// </summary>
    Message,

    /// <summary>
    /// The log entry is an event.
    /// </summary>
    Event,

    /// <summary>
    /// The log entry is an exception.
    /// </summary>
    Exception
}
