using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Configuration;

/// <summary>
/// Represents a <see cref="Log"/> configuration.
/// </summary>
public partial class LoggerConfiguration
{
    /// <summary>
    /// Creates a new instance of the <see cref="LoggerConfiguration"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="LoggerConfiguration"/> class.</returns>
    public static partial LoggerConfiguration Create();

    /// <summary>
    /// Specifies that the provided <paramref name="sink"/> should be used by the <see cref="Log"/>.
    /// </summary>
    /// <param name="sink">The <see cref="ILogSink"/> to be used by the <see cref="Log"/>.</param>
    /// <returns>This <see cref="LoggerConfiguration"/> instance to enable fluent configuration.</returns>
    public partial LoggerConfiguration AddSink(ILogSink sink);

    /// <summary>
    /// <inheritdoc cref="AddSink"/>
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="ILogSink"/> to be used.</typeparam>
    /// <returns>This <see cref="LoggerConfiguration"/> instance to enable fluent configuration.</returns>
    public partial LoggerConfiguration AddSink<T>() where T : ILogSink, new();

    /// <summary>
    /// Specifies that the provided <paramref name="logWriter"/> should be used by the <see cref="Log"/> as the default writer.
    /// </summary>
    /// <param name="logWriter">The <see cref="ILogWriter"/> to be used by the <see cref="Log"/> as the default writer.</param>
    /// <returns>This <see cref="LoggerConfiguration"/> instance to enable fluent configuration.</returns>
    public partial LoggerConfiguration UseDefaultLogWriter(ILogWriter logWriter);

    /// <summary>
    /// Specifies that the <see cref="Log"/> should log to a log file with the provided <paramref name="logFileName"/>.
    /// </summary>
    /// <param name="logFileName">The name of the file to log to.</param>
    /// <returns>An instance of <see cref="LogFileConfigurationBuilder"/> to configure the log file.</returns>
    public partial LogFileConfigurationBuilder UseLogFile(string logFileName);

    /// <summary>
    /// Specifies that the provided <paramref name="thread"/> is the application main thread and should be marked as such in the logs.
    /// </summary>
    /// <param name="thread">The application main thread.</param>
    /// <returns>This <see cref="LoggerConfiguration"/> instance to enable fluent configuration.</returns>
    public partial LoggerConfiguration RegisterMainThread(Thread thread);
}
