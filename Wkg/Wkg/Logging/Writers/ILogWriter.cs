﻿using Wkg.Logging.Sinks;

namespace Wkg.Logging.Writers;

/// <summary>
/// Represents a log writer specifying how log entries are written to an <see cref="ILogSink"/>.
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// Writes the <paramref name="logEntry"/> with the specified <paramref name="logLevel"/> to the <paramref name="sink"/>.
    /// </summary>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="sink">The <see cref="ILogSink"/> to write the log entry to.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> of the log entry.</param>
    void Write(string logEntry, ILogSink sink, LogLevel logLevel);
}

/// <summary>
/// Provides access to common <see cref="ILogWriter"/> implementations.
/// </summary>
public static class LogWriter
{
    /// <summary>
    /// Gets an <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> on the current thread.
    /// </summary>
    public static ILogWriter Blocking { get; } = new BlockingLogWriter();

    /// <summary>
    /// Gets an <see cref="ILogWriter"/> that writes log entries to the <see cref="ILogSink"/> asynchronously on a background thread.
    /// </summary>
    public static ILogWriter Background { get; } = new BackgroundLogWriter();
}