using Wkg.Logging;
using Wkg.Logging.Sinks;

namespace ConsoleApp1;

internal class ColoredThreadBasedConsoleSink : ILogSink
{
    private static readonly ReadOnlyMemory<ConsoleColor> _consoleColors = new ConsoleColor[]
    {
        ConsoleColor.DarkBlue,
        ConsoleColor.DarkGreen,
        ConsoleColor.DarkCyan,
        ConsoleColor.DarkRed,
        ConsoleColor.DarkMagenta,
        ConsoleColor.DarkYellow,
        ConsoleColor.Gray,
        ConsoleColor.DarkGray,
        ConsoleColor.Blue,
        ConsoleColor.Green,
        ConsoleColor.Cyan,
        ConsoleColor.Red,
        ConsoleColor.Magenta,
        ConsoleColor.Yellow,
        ConsoleColor.White,
    };

    private static readonly object _lock = new();
    
    public void Log(ref LogEntry logEntry)
    {
        lock (_lock)
        {
            int color = logEntry.ThreadId % _consoleColors.Length;
            Console.ForegroundColor = _consoleColors.Span[color];
            Console.WriteLine(logEntry);
            Console.ResetColor();
        }
    }
}
