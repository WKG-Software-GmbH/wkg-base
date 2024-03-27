namespace Wkg.Logging.Sinks;

/// <summary>
/// An <see cref="ILogSink"/> that writes log entries to the console using colors to indicate the <see cref="LogLevel"/>.
/// </summary>
/// <remarks>
/// <see langword="WARNING"/>: This sink is incompatible with other sinks that log to the console.
/// </remarks>
public class ColoredConsoleSink : ILogSink
{
    private static readonly object _lock = new();

    private static readonly ConsoleColor[] _colorsByLogLevel =
    [
        ConsoleColor.DarkGray,
        ConsoleColor.White,
        ConsoleColor.Green,
        ConsoleColor.Blue,
        ConsoleColor.Yellow,
        ConsoleColor.Red,
        ConsoleColor.Magenta,
    ];

    /// <inheritdoc/>
    public void Log(ref readonly LogEntry logEntry)
    {
        lock (_lock)
        {
            LogUnsafe(in logEntry);
        }
    }

    /// <inheritdoc/>
    public void LogUnsafe(ref readonly LogEntry logEntry)
    {
        Console.ForegroundColor = ColorFor(logEntry.LogLevel);
        Console.WriteLine(logEntry);
        Console.ResetColor();
    }

    private static ConsoleColor ColorFor(LogLevel level)
    {
        if (Enum.IsDefined(level))
        {
            return _colorsByLogLevel[(int)level];
        }
        return ConsoleColor.Cyan;
    }
}
