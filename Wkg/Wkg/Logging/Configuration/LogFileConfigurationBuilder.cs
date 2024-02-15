using Wkg.Logging.Sinks;

namespace Wkg.Logging.Configuration;

/// <summary>
/// Builds a configuration for to-file logging
/// </summary>
public class LogFileConfigurationBuilder
{
    private readonly LoggerConfiguration _config;
    private readonly string _logFileName;
    private long _maxLogFileSize = 8_388_608L; // 8 MiB
    private object? _syncRoot;

    internal LogFileConfigurationBuilder(string logFileName, LoggerConfiguration config)
    {
        _config = config;
        _logFileName = logFileName;
    }

    /// <summary>
    /// Specifies the maximum file size in bytes of the log file before it will be truncated to save disk space.
    /// </summary>
    /// <remarks>The default size is 8 MiB.</remarks>
    /// <param name="maxByteCount">The maximum file size in bytes of the log file before it will be truncated to save disk space. Must be larger than 0.</param>
    /// <returns>This instance of the <see cref="LogFileConfigurationBuilder"/> to enable configuration chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="maxByteCount"/> is less than or equal to 0.</exception>
    public LogFileConfigurationBuilder WithMaxFileSize(long maxByteCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxByteCount, 0, nameof(maxByteCount));

        _maxLogFileSize = maxByteCount;
        return this;
    }

    /// <summary>
    /// Specifies the object to use as a lock for file access.
    /// </summary>
    /// <param name="syncRoot">The object to use as a lock for file access.</param>
    /// <returns>This instance of the <see cref="LogFileConfigurationBuilder"/> to enable configuration chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="syncRoot"/> is <see langword="null"/>.</exception>
    public LogFileConfigurationBuilder WithSyncRoot(object syncRoot)
    {
        ArgumentNullException.ThrowIfNull(syncRoot, nameof(syncRoot));
        _syncRoot = syncRoot;
        return this;
    }

    /// <summary>
    /// Builds and saves the log file configuration to an <see cref="ILogSink"/> of the parent <see cref="LoggerConfiguration"/>.
    /// </summary>
    /// <returns>The parent <see cref="LoggerConfiguration"/> to enable configuration chaining.</returns>
    public LoggerConfiguration BuildToConfig()
    {
        ILogSink sink = new LogFileSink(_logFileName, _maxLogFileSize, _syncRoot);
        _config.AddSink(sink);
        return _config;
    }
}
