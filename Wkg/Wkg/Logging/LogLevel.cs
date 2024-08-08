using Wkg.Logging.Sinks;
using Wkg.Logging.Writers;

namespace Wkg.Logging;

/// <summary>
/// Specifies the meaning and relative importance of a log event.
/// </summary>
public enum LogLevel : uint
{
    /// <summary>
    /// Function-level debugging and unit intrinsics
    /// <para>
    /// The most verbose logging level. Use this level for internal system events that may be useful for debugging of application intrinsics. 
    /// Relevance of these events is often limited to the immediate scope of the call site, and is likely not of interest outside of the immediate method or class.
    /// </para>
    /// </summary>
    Diagnostic = 0,

    /// <summary>
    /// Component-level debugging and integration intrinsics
    /// <para>
    /// Use this level for debugging information. This level is used for debugging information that may be useful to developers. 
    /// This level is typically used for logging of debugging information that is relevant for debugging the integration of different components of the application. 
    /// As such, no highly specific component intrinsics should be logged at this level.
    /// </para>
    /// </summary>
    Debug = 1,

    /// <summary>
    /// Input event logging
    /// <para>
    /// Use this level for logging of events that are relevant to the overall program flow. 
    /// Event-level log entries often correspond to external events or user activity, such as button clicks, form submissions, 
    /// or other user interactions that may be relevant to tracing back the user's actions in the application in post-mortem analysis. 
    /// In multi-user applications, event-level log entries are often exchanged with <see cref="Diagnostic"/> or <see cref="Debug"/> log entries to reduce the amount of log data generated. 
    /// <see cref="Event"/> log entries may also be used to log non-interactive events in the application, as long as they are relevant to the overall program flow 
    /// and do not qualify as <see cref="Diagnostic"/> application intrinsics.
    /// </para>
    /// </summary>
    Event = 2,

    /// <summary>
    /// Informational logging with global relevance
    /// <para>
    /// Use this level for logging of informational messages. Informational log entries are used to provide information about the application's state, 
    /// configuration, or other relevant information that may be useful outside of debugging scenarios. Entries of this level often include initialization messages, 
    /// the loaded configurations, and other information that may be useful for understanding the application's behavior. 
    /// </para>
    /// The desciminating factor between <see cref="Info"/> and and lower log entries is that <see cref="Info"/> log entries have a higher relavance to the system and should not 
    /// require intricate knowledge of the application's flow or structure to be understood.
    /// </summary>
    Info = 3,

    /// <summary>
    /// Service is degraded or endangered.
    /// <para>
    /// Use this level for logging of warning messages. 
    /// Log messages of the warning level indicate that an unexpected condition has occurred that may not be critical to the application's operation, but may require attention. 
    /// Warning messages *should not occur* during normal operation of the application, 
    /// and indicate that the application has entered a state that may be undesirable or that may lead to unexpected behavior. 
    /// They often indicate that a fallback mechanism has been used, that a deprecated feature has been used, or that a non-critical error has occurred.
    /// </para>
    /// </summary>
    Warning = 4,

    /// <summary>
    /// Functionality is unavailable
    /// <para>
    /// Use this level for logging of unhandled exceptions, errors, and other critical messages. 
    /// Error messages indicate that a critical error has occurred that may prevent the application from functioning correctly. 
    /// </para>
    /// Messages of this level indicate a need for triage and may require immediate attention. 
    /// Alerting and monitoring systems should be configured accordingly and integrated through <see cref="ILogSink"/> implementations.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Application is unusable
    /// <para>
    /// Fatal messages indicate that the application has entered a state that is unrecoverable, 
    /// often logged right before an undesired termination of the application with a non-zero exit code. 
    /// Ensure to use <see cref="LogWriter.Blocking"/> to prolong the application's lifetime to allow for the log entry to be written to the sinks before its untimely demise.
    /// </para>
    /// The last breath of your application before it dies a horrible and unexpected death.
    /// </summary>
    Fatal = 6,

    /// <summary>
    /// Global, unconditional logging
    /// <para>
    /// System messages ignore the configured log level and are always logged. 
    /// Use this level for logging of system messages that are always relevant, such as startup and shutdown messages, or messages that are always relevant, 
    /// such as the application's version number or version control information of the application's components. 
    /// </para>
    /// For obvious reasons, use this level sparingly.
    /// </summary>
    System = 7
}
