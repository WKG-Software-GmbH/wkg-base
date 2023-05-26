using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

public class SimpleLogEntryGenerator : ILogEntryGenerator<SimpleLogEntryGenerator>
{
    private readonly CompiledLoggerConfiguration _config;

    protected SimpleLogEntryGenerator(CompiledLoggerConfiguration config) => 
        _config = config;

    public static SimpleLogEntryGenerator Create(CompiledLoggerConfiguration config) => 
        new(config);

    public string Generate(string title, string message, LogLevel level) =>
        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC: ({level}) on {CurrentThreadToString()} --> {title}: \'{message}\'";

    public string Generate(Exception exception, LogLevel level) => 
        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC: ({level}) {exception.GetType().Name} on {CurrentThreadToString()} --> \'{exception.Message}\' at: \n{exception.StackTrace}";

    public string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs)
    {
        if (assemblyName is not null && className is not null)
        {
            return $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC: (Event) on {CurrentThreadToString()} --> ({assemblyName}) ({className}::{instanceName}) ==> {eventName}({eventArgs})";
        }
        else
        {
            return $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC: (Event) on {CurrentThreadToString()} --> ({instanceName}) ==> {eventName}({eventArgs})";
        }
    }

    protected virtual string CurrentThreadToString()
    {
        int threadId = Environment.CurrentManagedThreadId;
        string threadName = $"Thread_0x{threadId:x}";
        if (threadId == _config.MainThreadId)
        {
            return $"{threadName} (Main Thread)";
        }
        return threadName;
    }
}
