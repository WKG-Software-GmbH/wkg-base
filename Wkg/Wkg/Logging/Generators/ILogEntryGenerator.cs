using Wkg.Logging.Configuration;
using Wkg.Logging.Sinks;

namespace Wkg.Logging.Generators;

/// <summary>
/// Represents a type that transforms known data and context of a log entry into a string that can be written to an <see cref="ILogSink"/>.
/// </summary>
public interface ILogEntryGenerator
{
    /// <summary>
    /// Generates a log entry from the specified <paramref name="title"/> and <paramref name="message"/>.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to write the log entry to.</param>
    /// <param name="title">The title of the log entry.</param>
    /// <param name="message">The message of the log entry.</param>
    /// <returns>The log entry string.</returns>
    void Generate(ref LogEntry entry, string title, string message);

    /// <summary>
    /// Generates a log entry from the specified <paramref name="exception"/> and <paramref name="additionalInfo"/>.
    /// </summary>
    /// <param name="entry">The <see cref="LogEntry"/> to write the log entry to.</param>
    /// <param name="exception">The <see cref="Exception"/> to generate the log entry string from.</param>
    /// <param name="additionalInfo">Additional information about the exception to include in the log entry string.</param>
    void Generate(ref LogEntry entry, Exception exception, string? additionalInfo);

    /// <summary>
    /// Generates a log entry from an event using the available details of the originating event.
    /// </summary>
    /// <typeparam name="TEventArgs">The type of the event arguments.</typeparam>
    /// <param name="entry">The <see cref="LogEntry"/> to write the log entry to.</param>
    /// <param name="assemblyName">The name of the assembly that raised the event.</param>
    /// <param name="className">The name of the class that raised the event.</param>
    /// <param name="instanceName">The name of the instance that raised the event.</param>
    /// <param name="eventName">The name of the event that was raised.</param>
    /// <param name="eventArgs">The event arguments.</param>
    void Generate<TEventArgs>(ref LogEntry entry, string? assemblyName, string? className, string instanceName, string eventName, TEventArgs eventArgs);
}

/// <summary>
/// An <see cref="ILogEntryGenerator"/> providing a factory method for creating instances of the generator.
/// </summary>
/// <typeparam name="TGenerator">The type of the generator.</typeparam>
public interface ILogEntryGenerator<out TGenerator> : ILogEntryGenerator where TGenerator : class, ILogEntryGenerator
{
    /// <summary>
    /// Creates a new instance of the generator using the specified <paramref name="config"/>.
    /// </summary>
    /// <param name="config">The <see cref="CompiledLoggerConfiguration"/> to use when creating the generator.</param>
    static abstract TGenerator Create(CompiledLoggerConfiguration config);
}
