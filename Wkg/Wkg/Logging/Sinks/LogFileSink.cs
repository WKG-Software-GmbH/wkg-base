using System.Text;

namespace Wkg.Logging.Sinks;

/// <summary>
/// Logs messages to a file.
/// </summary>
public class LogFileSink : ILogSink
{
    /// <summary>
    /// Lock object for file access.
    /// </summary>
    private readonly object _syncRoot;

    /// <summary>
    /// Wrapper for <see cref="File.AppendAllLines(string, IEnumerable{string}, Encoding)"/>.
    /// </summary>
    private readonly string[] _writeLineWrapper = new string[1];

    /// <summary>
    /// Name of the log file.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Maximum size of the log file in bytes.
    /// </summary>
    public long MaxFileSize { get; }

    /// <summary>
    /// Creates a new <see cref="LogFileSink"/>.
    /// </summary>
    /// <param name="logFileName">Name of the log file.</param>
    /// <param name="logFileMaxByteSize">Maximum size of the log file in bytes.</param>
    /// <param name="syncRoot">Lock object for file access.</param>
    internal LogFileSink(string logFileName, long logFileMaxByteSize, object? syncRoot)
    {
        FileName = logFileName;
        MaxFileSize = logFileMaxByteSize;
        _syncRoot = syncRoot ?? new object();
        new FileInfo(FileName).Directory?.Create();
    }

    /// <summary>
    /// Logs a message to the file.
    /// </summary>
    /// <param name="logEntry">Message to log.</param>
    public void Log(ref readonly LogEntry logEntry)
    {
        lock (_syncRoot)
        {
            LogUnsafe(in logEntry);
        }
    }

    public void LogUnsafe(ref readonly LogEntry logEntry)
    {
        TruncateFile__UNSAFE();
        _writeLineWrapper[0] = logEntry.LogMessage;
        File.AppendAllLines(FileName, _writeLineWrapper, Encoding.UTF8);
    }

    /// <summary>
    /// Truncates the log file if it is too large.
    /// </summary>
    private void TruncateFile__UNSAFE()
    {
        if (File.Exists(FileName) && new FileInfo(FileName).Length > MaxFileSize)
        {
            string[] logEntries = File.ReadAllLines(FileName);
            _writeLineWrapper[0] = "<<<------------------------ FILE TRUNCATED ------------------------>>>";
            File.WriteAllLines(FileName, _writeLineWrapper);
            File.AppendAllLines(FileName, logEntries[(logEntries.Length / 2)..]);
        }
    }
}
