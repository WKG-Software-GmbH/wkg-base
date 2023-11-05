using Wkg.Logging.Generators;
using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging.Configuration;

/// <summary>
/// A compiled, readonly logger configuration.
/// </summary>
/// <param name="LoggingSinks">The log sinks to use for logging.</param>
/// <param name="MainThreadId">The <see cref="Thread.ManagedThreadId"/> of the main thread.</param>
/// <param name="DefaultLogWriter">The default <see cref="ILogWriter"/> to use for logging.</param>
/// <param name="GeneratorFactory">A factory function that creates an <see cref="ILogEntryGenerator"/> from a <see cref="CompiledLoggerConfiguration"/>.</param>
public record CompiledLoggerConfiguration
(
    LogLevel MinimumLogLevel,
    ConcurrentSinkCollection LoggingSinks, 
    int MainThreadId, 
    ILogWriter DefaultLogWriter,
    Func<CompiledLoggerConfiguration, ILogEntryGenerator> GeneratorFactory
);