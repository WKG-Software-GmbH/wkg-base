using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Wkg.Threading;

/// <summary>
/// The exception that is thrown when an operation is performed on a disposed <see cref="AsyncLock"/>.
/// </summary>
public sealed class LockDisposedException : ObjectDisposedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockDisposedException"/> class.
    /// </summary>
    public LockDisposedException() : base(objectName: null) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockDisposedException"/> class with the specified object name.
    /// </summary>
    /// <param name="objectName">The name of the disposed object.</param>
    public LockDisposedException(string? objectName) : base(objectName) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockDisposedException"/> class with the specified object name and message.
    /// </summary>
    /// <param name="objectName">The name of the disposed object.</param>
    /// <param name="message">The message that describes the error.</param>
    public LockDisposedException(string? objectName, string? message) : base(objectName, message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockDisposedException"/> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public LockDisposedException(string message, Exception innerException) : base(message, innerException) { }

    [StackTraceHidden]
    internal static new void ThrowIf([DoesNotReturnIf(true)] bool condition, object instance)
    {
        if (condition)
        {
            ThrowObjectDisposedException(instance);
        }
    }

    [StackTraceHidden]
    [DoesNotReturn]
    internal static void Rethrow(ObjectDisposedException inner) => throw new LockDisposedException("The lock was disposed.", inner);

    [StackTraceHidden]
    [DoesNotReturn]
    internal static void ThrowObjectDisposedException(object? instance) => throw new LockDisposedException(instance?.GetType().FullName);
}