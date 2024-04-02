namespace Wkg.Logging.Sinks;

/// <summary>
/// A sink that writes to the console.
/// </summary>
/// <remarks>
/// <see langword="WARNING"/>: This sink is incompatible with other sinks that log to the console.
/// </remarks>
public class ConsoleSink : ILogSink
{
    /// <inheritdoc/>
    public void Log(ref readonly LogEntry logEntry) =>
        Console.WriteLine(logEntry.LogMessage);

    /// <inheritdoc/>
    public void LogUnsafe(ref readonly LogEntry logEntry) =>
        Console.WriteLine(logEntry.LogMessage);
}