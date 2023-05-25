using Wkg.Logging.Writers;

namespace Wkg.Logging;

public interface ILogger
{
    void Log(string message, LogLevel logLevel = LogLevel.Debug);

    void Log(string message, ILogWriter logWriter, LogLevel logLevel = LogLevel.Debug);

    void Log(Exception exception, LogLevel logLevel = LogLevel.Error);

    void Log(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, string? assemblyName = null, string? className = null);

    void Log<TEventArgs>(string instanceName, string eventName, TEventArgs eventArgs, ILogWriter logWriter, string? assemblyName = null, string? className = null);
}
