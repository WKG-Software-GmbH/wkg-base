﻿using Wkg.Logging.Generators;

namespace Wkg.Logging;

/// <summary>
/// Represents a log entry.
/// </summary>
public struct LogEntry
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the log entry type.
    /// </summary>
    public LogEntryType Type { get; set; }

    /// <summary>
    /// Gets or sets the thread ID.
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the log entry was generated by the main thread.
    /// </summary>
    public bool IsMainThread { get; set; }

    /// <summary>
    /// Gets or sets the final constructed log message.
    /// </summary>
    public string LogMessage { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the log entry.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets the caller information from which the log entry originated.
    /// </summary>
    public CallerInfo CallerInfo { get; set; }

    /// <summary>
    /// Gets or sets the name of the assembly that the log entry originated from.
    /// </summary>
    /// <remarks>
    /// For log entries originating from events, this property may be pre-populated with the assembly name before the log entry is passed to the configured <see cref="ILogEntryGenerator"/>.
    /// </remarks>
    public string? AssemblyName { get; set; }

    /// <summary>
    /// Gets or sets the name of the class that the log entry originated from.
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// Gets or sets the name of the class instance that the log entry originated from.
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Gets or sets the name of the event that the log entry originated from.
    /// </summary>
    public string? EventName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Exception"/> that the log entry originated from.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the event arguments that are associated with the log entry.
    /// </summary>
    public object? EventArgs { get; set; }

    /// <summary>
    /// Gets or sets the target site of the log entry.
    /// </summary>
    public string? TargetSite { get; set; }

    /// <summary>
    /// Gets or sets the additional information of the log entry.
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <inheritdoc/>
    public readonly override string ToString() => LogMessage;
}
