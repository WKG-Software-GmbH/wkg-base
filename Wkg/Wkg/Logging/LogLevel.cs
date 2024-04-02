namespace Wkg.Logging;

/// <summary>
/// Specifies the meaning and relative importance of a log event.
/// </summary>
public enum LogLevel : uint
{
    /// <summary>
    /// Internal system events that may be useful for debugging of application intrinsics.
    /// </summary>
    Diagnostic = 0,

    /// <summary>
    /// System events that may be useful for debugging.
    /// </summary>
    Debug = 1,

    /// <summary>
    /// The user did a thing to trigger an input event.
    /// </summary>
    Event = 2,

    /// <summary>
    /// Things happen. The kind of things you want to know about regardless of what you're currently working on like important changes of the application state.
    /// </summary>
    Info = 3,

    /// <summary>
    /// Service is degraded or endangered. Something's not entirely working as intended but the application is not crashing either (yet).
    /// </summary>
    Warning = 4,

    /// <summary>
    /// Functionality is unavailable, invariants are broken, data is lost and something went wrong somewhere.
    /// </summary>
    Error = 5,

    /// <summary>
    /// The last breath of your application before it dies a horrible and unexpected death.
    /// </summary>
    Fatal = 6,
}
