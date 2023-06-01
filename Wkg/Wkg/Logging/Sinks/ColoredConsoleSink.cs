namespace Wkg.Logging.Sinks;

/// <summary>
/// An <see cref="ILogSink"/> that writes log entries to the console using colors to indicate the <see cref="LogLevel"/>.
/// </summary>
public class ColoredConsoleSink : ILogSink
{
    private static readonly object _lock = new();

    /// <inheritdoc/>
    public void Log(string logEntry, LogLevel logLevel)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ColorFor(logLevel);
            Console.WriteLine(logEntry);
            Console.ResetColor();
        }
    }

    private static ConsoleColor ColorFor(LogLevel level) => level switch
    {
        LogLevel.Diagnostic => ConsoleColor.DarkGray,
        LogLevel.Debug => ConsoleColor.White,
        LogLevel.Event => ConsoleColor.Green,
        LogLevel.Info => ConsoleColor.Blue,
        LogLevel.Warning => ConsoleColor.Yellow,
        LogLevel.Error => ConsoleColor.Red,
        LogLevel.Fatal => ConsoleColor.Magenta,
        _ => ConsoleColor.Cyan
    };
}
