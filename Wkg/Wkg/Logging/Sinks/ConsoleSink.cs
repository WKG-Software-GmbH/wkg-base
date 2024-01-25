﻿namespace Wkg.Logging.Sinks;

/// <summary>
/// A sink that writes to the console.
/// </summary>
/// <remarks>
/// <see langword="WARNING"/>: This sink is incompatible with other sinks that log to the console.
/// </remarks>
public class ConsoleSink : ILogSink
{
    /// <summary>
    /// Logs the specified message.
    /// </summary>
    /// <param name="logEntry">The message to be logged.</param>
    public void Log(ref LogEntry logEntry) =>
        Console.WriteLine(logEntry.LogMessage);
}