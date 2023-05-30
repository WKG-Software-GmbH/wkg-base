using Wkg.Logging.Configuration;

namespace Wkg.Logging.Generators;

public interface ILogEntryGenerator
{
    string Generate(string title, string message, LogLevel level);

    string Generate(Exception exception, string? additionalInfo, LogLevel level);

    string Generate<TEventArgs>(string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs);
}

public interface ILogEntryGenerator<out TGenerator> : ILogEntryGenerator where TGenerator : class, ILogEntryGenerator
{
    static abstract TGenerator Create(CompiledLoggerConfiguration config);
}
