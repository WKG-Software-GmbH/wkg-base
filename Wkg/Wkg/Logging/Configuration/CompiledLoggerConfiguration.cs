using Wkg.Logging.Entries;
using Wkg.Logging.Sinks;

namespace Wkg.Logging.Configuration;

/// <summary>
/// A compiled, readonly logger configuration.
/// </summary>
/// <param name="LoggingSinks">The log sinks to use for logging.</param>
/// <param name="MainThreadId">The <see cref="Thread.ManagedThreadId"/> of the main thread.</param>
public record CompiledLoggerConfiguration(ConcurrentSinkCollection LoggingSinks, int MainThreadId, Func<CompiledLoggerConfiguration, ILogEntryGenerator> GeneratorFactory);