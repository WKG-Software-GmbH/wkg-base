using System.Collections.Immutable;

namespace Wkg.Logging.Generators.Helpers;

/// <summary>
/// Provides human-readable names for <see cref="LogLevel"/> values.
/// </summary>
public static class LogLevelNames
{
    private static readonly ImmutableArray<string> _logLevelNames;

    static LogLevelNames()
    {
        string[] logLevelNames = new string[Enum.GetValues<LogLevel>().Length];
        for (uint i = 0; i < logLevelNames.Length; i++)
        {
            LogLevel logLevel = (LogLevel)i;
            logLevelNames[i] = logLevel.ToString();
            if (logLevel is LogLevel.Error or LogLevel.Fatal)
            {
                logLevelNames[i] = logLevelNames[i].ToUpperInvariant();
            }
        }
        _logLevelNames = [.. logLevelNames];
    }

    /// <summary>
    /// Retrieves the human-readable name for the specified <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="logLevel">The <see cref="LogLevel"/> to retrieve the name for.</param>
    public static string NameFor(LogLevel logLevel) => _logLevelNames[(int)logLevel];

    /// <summary>
    /// Retrieves the human-readable name for the specified <see cref="LogLevel"/> or "UNKNOWN" if the value is not defined.
    /// </summary>
    /// <param name="logLevel">The <see cref="LogLevel"/> to retrieve the name for.</param>
    public static string NameForOrUnknown(LogLevel logLevel) => Enum.IsDefined(logLevel) ? NameFor(logLevel) : "UNKNOWN";
}
