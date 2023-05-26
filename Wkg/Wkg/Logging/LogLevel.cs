namespace Wkg.Logging;

/// <summary>
/// Specifies the meaning and relative importance of a log event.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Internal system events that may be useful for debugging of application intrinsics.
    /// </summary>
    Diagnostic,

    /// <summary>
    /// System events that may be useful for debugging.
    /// </summary>
    Debug,

    /// <summary>
    /// Functionality is unavailable, invariants are broken, data is lost and something went wrong somewhere.
    /// </summary>
    Error,

    /// <summary>
    /// The last breath of your application before it dies a horrible and unexpected death.
    /// </summary>
    Fatal,

    /// <summary>
    /// Things happen. The kind of things you want to know about regardless of what you're currently working on like important changes of the application state.
    /// </summary>
    Info,

    /// <summary>
    /// Service is degraded or endangered. Something's not entirely working as intended but the application is not crashing either (yet).
    /// </summary>
    Warning,

    /// <summary>
    /// The user did a thing to trigger an input event.
    /// </summary>
    Event
}
