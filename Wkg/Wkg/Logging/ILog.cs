using Wkg.Logging.Loggers;
using Wkg.Logging.Writers;

namespace Wkg.Logging;

public interface ILog
{
    static abstract ILogger CurrentLogger { get; }

    static abstract void UseLogger(ILogger logger);

    static abstract void WriteDiagnostic(string message, ILogWriter logWriter);

    static abstract void WriteDiagnostic(string message);

    static abstract void WriteDebug(string message);

    static abstract void WriteDebug(string message, ILogWriter logWriter);

    static abstract void WriteInfo(string message);

    static abstract void WriteInfo(string message, ILogWriter logWriter);

    static abstract void WriteWarning(string message);

    static abstract void WriteWarning(string message, ILogWriter logWriter);

    static abstract void WriteError(string message);

    static abstract void WriteError(string message, ILogWriter logWriter);

    static abstract void WriteFatal(string message);

    static abstract void WriteFatal(string message, ILogWriter logWriter);

    static abstract void WriteException(Exception exception, LogLevel logLevel = LogLevel.Error);

    static abstract void WriteException(Exception exception, ILogWriter logWriter, LogLevel logLevel = LogLevel.Error);

    static abstract void WriteEvent(string message);

    static abstract void WriteEvent(string message, ILogWriter logWriter);
}
