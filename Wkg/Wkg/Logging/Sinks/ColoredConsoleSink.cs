namespace Wkg.Logging.Sinks;

/// <summary>
/// An <see cref="ILogSink"/> that writes log entries to the console using colors to indicate the <see cref="LogLevel"/>.
/// </summary>
/// <remarks>
/// <see langword="WARNING"/>: This sink is incompatible with other sinks that log to the console.
/// </remarks>
public class ColoredConsoleSink : ILogSink
{
    private static readonly Lock s_lock = new();

    private static readonly ConsoleColor[] s_colorsByLogLevel =
    [
        ConsoleColor.DarkGray,
        ConsoleColor.White,
        ConsoleColor.Green,
        ConsoleColor.Blue,
        ConsoleColor.Yellow,
        ConsoleColor.Red,
        ConsoleColor.Magenta,
        ConsoleColor.Cyan,
    ];

    /// <inheritdoc/>
    public void Log(ref readonly LogEntry logEntry)
    {
        lock (s_lock)
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
            return s_colorsByLogLevel[(int)level];
        }
        return ConsoleColor.Cyan;
    }
}
