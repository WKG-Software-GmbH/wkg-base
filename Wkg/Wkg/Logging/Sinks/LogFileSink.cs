using System.Text;

namespace Wkg.Logging.Sinks;

/// <summary>
/// Logs messages to a file.
/// </summary>
internal class LogFileSink : ILogSink
{
    /// <summary>
    /// Lock object for file access.
    /// </summary>
    private readonly object _fileLock = new();

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
    internal LogFileSink(string logFileName, long logFileMaxByteSize)
    {
        FileName = logFileName;
        MaxFileSize = logFileMaxByteSize;
        new FileInfo(FileName).Directory?.Create();
    }

    /// <summary>
    /// Logs a message to the file.
    /// </summary>
    /// <param name="entry">Message to log.</param>
    /// <param name="logLevel">The <see cref="LogLevel"/> of the message.</param>
    public void Log(string entry, LogLevel logLevel)
    {
        lock (_fileLock)
        {
            TruncateFile__UNSAFE();
            _writeLineWrapper[0] = entry;
            File.AppendAllLines(FileName, _writeLineWrapper, Encoding.UTF8);
        }
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
