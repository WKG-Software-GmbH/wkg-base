namespace Wkg.Logging.Sinks;

/// <summary>
/// A thread-safe console sink that colors each thread's output differently. 
/// Very useful for debugging threading issues with log trace points.
/// </summary>
/// <remarks>
/// <see langword="WARNING"/>: This sink is incompatible with other sinks that log to the console.<br></br>
/// This sink does not guarantee that every thread will have a different color (there are only 10 usable unique console colors). 
/// Always rely on the thread id in the log entry, if you need to distinguish between threads.
/// </remarks>
public class ColoredThreadBasedConsoleSink : ILogSink
{
    private static readonly ConsoleColor[] s_consoleColors =
    [
        ConsoleColor.White,
        ConsoleColor.Blue,
        ConsoleColor.Green,
        ConsoleColor.Cyan,
        ConsoleColor.Red,
        ConsoleColor.Yellow,
        ConsoleColor.Magenta,
        ConsoleColor.DarkCyan,
        ConsoleColor.DarkYellow,
        ConsoleColor.DarkGray,
    ];

    private static readonly Lock s_lock = new();

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
        int color = logEntry.ThreadId % s_consoleColors.Length;
        Console.ForegroundColor = s_consoleColors[color];
        Console.WriteLine(logEntry);
        Console.ResetColor();
    }
}
